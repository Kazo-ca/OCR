using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace KazoOCR.Core;

/// <summary>
/// File system watcher service that processes new PDF files asynchronously.
/// </summary>
public sealed class WatcherService : IWatcherService
{
    private const int QueueCapacity = 1024;
    private const int ValidationRetryDelayMilliseconds = 200;
    private const int ValidationRetryMaxAttempts = 3;

    private readonly IOcrFileService _fileService;
    private readonly IOcrProcessRunner _processRunner;
    private readonly ILogger<WatcherService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatcherService"/> class.
    /// </summary>
    /// <param name="fileService">The OCR file service.</param>
    /// <param name="processRunner">The OCR process runner.</param>
    /// <param name="logger">The logger instance.</param>
    public WatcherService(IOcrFileService fileService, IOcrProcessRunner processRunner, ILogger<WatcherService> logger)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task WatchAsync(string inputDirectory, OcrSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inputDirectory);
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(inputDirectory))
        {
            throw new ArgumentException("Input directory cannot be empty.", nameof(inputDirectory));
        }

        if (!Directory.Exists(inputDirectory))
        {
            throw new DirectoryNotFoundException($"Directory not found: {inputDirectory}");
        }

        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(QueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropWrite
        });

        void OnCreated(object sender, FileSystemEventArgs eventArgs) => TryQueue(eventArgs.FullPath);
        void OnRenamed(object sender, RenamedEventArgs eventArgs) => TryQueue(eventArgs.FullPath);
        void OnError(object sender, ErrorEventArgs eventArgs) =>
            _logger.LogError(eventArgs.GetException(), "File system watcher error in {Directory}", inputDirectory);

        void TryQueue(string path)
        {
            if (!ShouldProcess(path, settings.Suffix))
            {
                return;
            }

            if (channel.Writer.TryWrite(path))
            {
                _logger.LogInformation("Queued PDF for processing: {File}", path);
                return;
            }

            _logger.LogWarning("Queue is full, dropping file event: {File}", path);
        }

        using var watcher = new FileSystemWatcher(inputDirectory, "*")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
        };

        watcher.Created += OnCreated;
        watcher.Renamed += OnRenamed;
        watcher.Error += OnError;

        using var registration = cancellationToken.Register(() => channel.Writer.TryComplete());
        watcher.EnableRaisingEvents = true;

        _logger.LogInformation("Watching directory {Directory} for new PDF files.", inputDirectory);

        try
        {
            await foreach (var filePath in channel.Reader.ReadAllAsync(cancellationToken))
            {
                await ProcessFileAsync(filePath, settings, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Watcher canceled for directory {Directory}.", inputDirectory);
            throw;
        }
        finally
        {
            watcher.EnableRaisingEvents = false;
            watcher.Created -= OnCreated;
            watcher.Renamed -= OnRenamed;
            watcher.Error -= OnError;

            _logger.LogInformation("Stopped watching directory {Directory}.", inputDirectory);
        }
    }

    private bool ShouldProcess(string path, string suffix)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (!path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(suffix) && _fileService.IsAlreadyProcessed(path, suffix))
        {
            _logger.LogDebug("Ignoring file with OCR suffix to avoid loop: {File}", path);
            return false;
        }

        return true;
    }

    private async Task ProcessFileAsync(string filePath, OcrSettings settings, CancellationToken cancellationToken)
    {
        if (_fileService.IsAlreadyProcessed(filePath, settings.Suffix))
        {
            _logger.LogDebug("Skipping already processed file: {File}", filePath);
            return;
        }

        if (!await ValidateInputWithRetriesAsync(filePath, cancellationToken))
        {
            return;
        }

        var outputPath = _fileService.ComputeOutputPath(filePath, settings.Suffix);

        try
        {
            var result = await _processRunner.RunAsync(settings, filePath, outputPath, cancellationToken);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Processed file: {Input} -> {Output}", filePath, outputPath);
                return;
            }

            _logger.LogError("OCR failed for {File}: {Error}", filePath, result.StandardError);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OCR processing canceled for {File}", filePath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while processing {File}", filePath);
        }
    }

    private async Task<bool> ValidateInputWithRetriesAsync(string filePath, CancellationToken cancellationToken)
    {
        ValidationResult? lastValidation = null;
        for (var attempt = 1; attempt <= ValidationRetryMaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lastValidation = _fileService.ValidateInput(filePath);
            if (lastValidation.IsValid)
            {
                return true;
            }

            if (!IsTransientValidationFailure(lastValidation) || attempt == ValidationRetryMaxAttempts)
            {
                break;
            }

            _logger.LogDebug("Validation failed for {File}; retrying attempt {Attempt}/{MaxAttempts}", filePath, attempt + 1, ValidationRetryMaxAttempts);
            await Task.Delay(ValidationRetryDelayMilliseconds, cancellationToken);
        }

        _logger.LogWarning("Skipping invalid input file: {File}. Errors: {Errors}", filePath, string.Join("; ", lastValidation?.Errors ?? []));
        return false;
    }

    private static bool IsTransientValidationFailure(ValidationResult validationResult) =>
        validationResult.Errors.Any(error =>
            error.Contains("Cannot access file", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("Access denied", StringComparison.OrdinalIgnoreCase));
}
