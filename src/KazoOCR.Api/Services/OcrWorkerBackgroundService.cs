using KazoOCR.Core;
using static KazoOCR.Api.ApiConfiguration;

namespace KazoOCR.Api.Services;

/// <summary>
/// Background service that monitors a directory for new PDF files (embedded worker).
/// </summary>
public sealed class OcrWorkerBackgroundService : BackgroundService
{
    private readonly IWatcherService _watcherService;
    private readonly ILogger<OcrWorkerBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="OcrWorkerBackgroundService"/> class.
    /// </summary>
    public OcrWorkerBackgroundService(
        IWatcherService watcherService,
        ILogger<OcrWorkerBackgroundService> logger,
        IConfiguration configuration)
    {
        _watcherService = watcherService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var watchPath = GetWatchPath();
        var settings = BuildOcrSettings(_configuration);

        _logger.LogInformation(
            "OcrWorkerBackgroundService starting — WatchPath={WatchPath}, Suffix={Suffix}, Languages={Languages}, Deskew={Deskew}, Clean={Clean}, Rotate={Rotate}, Optimize={Optimize}",
            watchPath,
            settings.Suffix,
            settings.Languages,
            settings.Deskew,
            settings.Clean,
            settings.Rotate,
            settings.Optimize);

        // Check if watch path exists
        if (!Directory.Exists(watchPath))
        {
            _logger.LogWarning("Watch path does not exist: {WatchPath}. Worker will wait for directory to be created.", watchPath);

            // Wait for directory to exist
            while (!Directory.Exists(watchPath) && !stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
            }

            if (stoppingToken.IsCancellationRequested)
                return;

            _logger.LogInformation("Watch path now exists: {WatchPath}", watchPath);
        }

        try
        {
            await _watcherService.WatchAsync(watchPath, settings, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("OcrWorkerBackgroundService stopped gracefully");
        }
    }

    internal string GetWatchPath() =>
        GetConfigValue(_configuration, EnvWatchPath, DefaultWatchPath);
}
