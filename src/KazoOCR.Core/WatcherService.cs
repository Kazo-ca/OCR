using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace KazoOCR.Core;

/// <summary>
/// Watches a directory for new PDF files and processes them via OCR using an async channel queue.
/// Uses <see cref="FileSystemWatcher"/> for file events and <see cref="Channel{T}"/> for async processing.
/// </summary>
public sealed class WatcherService : IWatcherService
{
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

        _logger.LogInformation("Starting watcher on {WatchPath}", watchPath);

        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        using var watcher = CreateFileSystemWatcher(watchPath, settings.Suffix, channel.Writer);

        try
        {
            await ProcessChannelAsync(channel.Reader, settings, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _logger.LogInformation("Stopping watcher on {WatchPath}", watchPath);
        }
    }

    private FileSystemWatcher CreateFileSystemWatcher(
        string watchPath,
        string suffix,
        ChannelWriter<string> writer)
    {
        var watcher = new FileSystemWatcher(watchPath, "*.pdf")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        watcher.Created += (_, e) =>
        {
            if (_fileService.IsAlreadyProcessed(e.FullPath, suffix))
            {
                _logger.LogDebug("Skipping already processed file: {FilePath}", e.FullPath);
                return;
            }

            _logger.LogInformation("Detected new file: {FilePath}", e.FullPath);

            if (!writer.TryWrite(e.FullPath))
            {
                _logger.LogWarning("Failed to enqueue file: {FilePath}", e.FullPath);
            }
        };

        watcher.Error += (_, e) =>
        {
            _logger.LogError(e.GetException(), "FileSystemWatcher error");
        };

        return watcher;
    }

    private async Task ProcessChannelAsync(
        ChannelReader<string> reader,
        OcrSettings settings,
        CancellationToken cancellationToken)
    {
        await foreach (var filePath in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            await ProcessFileAsync(filePath, settings, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessFileAsync(string filePath, OcrSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            var outputPath = _fileService.ComputeOutputPath(filePath, settings.Suffix);
            _logger.LogInformation("Processing {InputPath} -> {OutputPath}", filePath, outputPath);

            var result = await _processRunner.RunAsync(settings, filePath, outputPath, cancellationToken).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully processed {InputPath}", filePath);
            }
            else
            {
                _logger.LogWarning(
                    "OCR failed for {InputPath} (exit code {ExitCode}): {Error}",
                    filePath,
                    result.ExitCode,
                    result.StandardError);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Processing cancelled for {FilePath}", filePath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing file {FilePath}", filePath);
        }
    }
}
