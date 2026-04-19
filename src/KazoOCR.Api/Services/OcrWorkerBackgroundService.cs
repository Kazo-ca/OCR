using KazoOCR.Core;

namespace KazoOCR.Api.Services;

/// <summary>
/// Background service that monitors a directory for new PDF files (embedded worker).
/// </summary>
public sealed class OcrWorkerBackgroundService : BackgroundService
{
    private readonly IWatcherService _watcherService;
    private readonly ILogger<OcrWorkerBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    internal const string EnvWatchPath = "KAZO_WATCH_PATH";
    internal const string EnvSuffix = "KAZO_SUFFIX";
    internal const string EnvLanguages = "KAZO_LANGUAGES";
    internal const string EnvDeskew = "KAZO_DESKEW";
    internal const string EnvClean = "KAZO_CLEAN";
    internal const string EnvRotate = "KAZO_ROTATE";
    internal const string EnvOptimize = "KAZO_OPTIMIZE";

    internal const string DefaultWatchPath = "/data";
    internal const string DefaultSuffix = "_OCR";
    internal const string DefaultLanguages = "fra+eng";
    internal const bool DefaultDeskew = true;
    internal const bool DefaultClean = false;
    internal const bool DefaultRotate = true;
    internal const int DefaultOptimize = 1;

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
        var settings = BuildOcrSettings();

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
        _configuration[EnvWatchPath]
        ?? Environment.GetEnvironmentVariable(EnvWatchPath)
        ?? DefaultWatchPath;

    internal OcrSettings BuildOcrSettings() => new()
    {
        Suffix = GetConfigValue(EnvSuffix, DefaultSuffix),
        Languages = GetConfigValue(EnvLanguages, DefaultLanguages),
        Deskew = ParseBool(GetConfigValue(EnvDeskew, null), DefaultDeskew),
        Clean = ParseBool(GetConfigValue(EnvClean, null), DefaultClean),
        Rotate = ParseBool(GetConfigValue(EnvRotate, null), DefaultRotate),
        Optimize = ParseInt(GetConfigValue(EnvOptimize, null), DefaultOptimize)
    };

    private string GetConfigValue(string key, string? defaultValue) =>
        _configuration[key]
        ?? Environment.GetEnvironmentVariable(key)
        ?? defaultValue
        ?? string.Empty;

    internal static bool ParseBool(string? value, bool defaultValue) =>
        bool.TryParse(value, out var result) ? result : defaultValue;

    internal static int ParseInt(string? value, int defaultValue) =>
        int.TryParse(value, out var result) ? result : defaultValue;
}
