using KazoOCR.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace KazoOCR.CLI;

/// <summary>
/// Background service for running multiple folder watchers simultaneously.
/// Used when the CLI runs as a Windows Service.
/// </summary>
public sealed class MultiWatcherBackgroundService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IWatcherService _watcherService;
    private readonly ILogger<MultiWatcherBackgroundService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiWatcherBackgroundService"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="watcherService">The watcher service.</param>
    /// <param name="logger">The logger.</param>
    public MultiWatcherBackgroundService(
        IConfiguration configuration,
        IWatcherService watcherService,
        ILogger<MultiWatcherBackgroundService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _watcherService = watcherService ?? throw new ArgumentNullException(nameof(watcherService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KazoOCR Service starting...");

        var watchFolders = _configuration.GetSection("WatchFolders").Get<List<WatchFolderConfig>>();

        if (watchFolders is null || watchFolders.Count == 0)
        {
            _logger.LogError("No watch folders configured. Please configure at least one folder in appsettings.service.json.");
            return;
        }

        _logger.LogInformation("Configured to watch {Count} folder(s).", watchFolders.Count);

        // Validate all folders exist
        var validFolders = new List<WatchFolderConfig>();
        foreach (var folder in watchFolders)
        {
            if (string.IsNullOrWhiteSpace(folder.Path))
            {
                _logger.LogWarning("Skipping empty path in configuration.");
                continue;
            }

            if (!Directory.Exists(folder.Path))
            {
                _logger.LogWarning("Skipping non-existent path: {Path}", folder.Path);
                continue;
            }

            validFolders.Add(folder);
            _logger.LogInformation("Will watch folder: {Path}", folder.Path);
        }

        if (validFolders.Count == 0)
        {
            _logger.LogError("No valid watch folders found. Please ensure the configured paths exist.");
            return;
        }

        // Start watchers for all valid folders
        var watcherTasks = validFolders.Select(folder =>
            WatchFolderAsync(folder, stoppingToken)).ToArray();

        try
        {
            await Task.WhenAll(watcherTasks);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("KazoOCR Service stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KazoOCR Service encountered an error.");
            throw;
        }
    }

    private async Task WatchFolderAsync(WatchFolderConfig config, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting watcher for folder: {Path}", config.Path);

        try
        {
            await _watcherService.WatchAsync(config.Path, config.ToOcrSettings(), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Stopped watching folder: {Path}", config.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error watching folder: {Path}", config.Path);
            throw;
        }
    }
}
