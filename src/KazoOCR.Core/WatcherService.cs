using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace KazoOCR.Core;

/// <summary>
/// Watches a directory for new PDF files and processes them via OCR using an async channel queue.
/// Uses <see cref="FileSystemWatcher"/> for file events and <see cref="Channel{T}"/> for async processing.
/// </summary>
public sealed class WatcherService : IWatcherService
{
    // Allows short event bursts while keeping memory usage bounded.
    // Increase only if sustained high-volume folder drops are expected.
    private const int QueueCapacity = 1024;
    private const int ValidationRetryDelayMilliseconds = 200;
    private const int ValidationRetryMaxAttempts = 3;

    private readonly IOcrFileService _fileService;
    private readonly IOcrProcessRunner _processRunner;
    private readonly ILogger<WatcherService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatcherService"/> class.
    /// </summary>
    /// <param name="fileService">The file service for path computation and validation.</param>
    /// <param name="processRunner">The process runner for OCR execution.</param>
    /// <param name="logger">The logger instance.</param>
    public WatcherService(
        IOcrFileService fileService,
        IOcrProcessRunner processRunner,
        ILogger<WatcherService> logger)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task WatchAsync(string watchPath, OcrSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(watchPath);
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(watchPath))
        {
            throw new ArgumentException("Watch path cannot be empty or whitespace.", nameof(watchPath));
        }

        if (!Directory.Exists(watchPath))
        {
            throw new DirectoryNotFoundException($"Watch directory does not exist: {watchPath}");
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
            _logger.LogError(eventArgs.GetException(), "File system watcher error in {Directory}", watchPath);

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

        using var watcher = new FileSystemWatcher(watchPath, "*")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
        };

        watcher.Created += OnCreated;
        watcher.Renamed += OnRenamed;
        watcher.Error += OnError;

        using var registration = cancellationToken.Register(() => channel.Writer.TryComplete());
        watcher.EnableRaisingEvents = true;

        _logger.LogInformation("Watching directory {Directory} for new PDF files.", watchPath);

        try
        {
            await foreach (var filePath in channel.Reader.ReadAllAsync(cancellationToken))
            {
                await ProcessFileAsync(filePath, settings, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Watcher canceled for directory {Directory}.", watchPath);
            throw;
        }
        finally
        {
            watcher.EnableRaisingEvents = false;
            watcher.Created -= OnCreated;
            watcher.Renamed -= OnRenamed;
            watcher.Error -= OnError;

            _logger.LogInformation("Stopped watching directory {Directory}.", watchPath);
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

            _logger.LogWarning(
                "OCR failed for {InputPath} (exit code {ExitCode}): {Error}",
                filePath,
                result.ExitCode,
                result.StandardError);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Processing cancelled for {FilePath}", filePath);
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

            _logger.LogDebug(
                "Validation failed for {File}; scheduling retry {NextAttempt}/{MaxAttempts}",
                filePath,
                attempt + 1,
                ValidationRetryMaxAttempts);
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
