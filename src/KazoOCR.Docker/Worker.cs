using KazoOCR.Core;

namespace KazoOCR.Docker;

/// <summary>
/// Background worker that monitors a directory for new PDF files and processes them via OCR.
/// Configuration is read from environment variables with sensible defaults.
/// </summary>
public sealed class Worker(
    IWatcherService watcherService,
    ILogger<Worker> logger) : BackgroundService
{
    internal const string EnvWatchPath = "KAZO_WATCH_PATH";
    internal const string EnvSuffix = "KAZO_SUFFIX";
    internal const string EnvLanguages = "KAZO_LANGUAGES";
    internal const string EnvDeskew = "KAZO_DESKEW";
    internal const string EnvClean = "KAZO_CLEAN";
    internal const string EnvRotate = "KAZO_ROTATE";
    internal const string EnvOptimize = "KAZO_OPTIMIZE";

    internal const string DefaultWatchPath = "/watch";
    internal const string DefaultSuffix = "_OCR";
    internal const string DefaultLanguages = "fra+eng";
    internal const bool DefaultDeskew = true;
    internal const bool DefaultClean = false;
    internal const bool DefaultRotate = true;
    internal const int DefaultOptimize = 1;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var watchPath = GetWatchPath();
        var settings = BuildOcrSettings();

        logger.LogInformation(
            "KazoOCR Worker starting — WatchPath={WatchPath}, Suffix={Suffix}, Languages={Languages}, Deskew={Deskew}, Clean={Clean}, Rotate={Rotate}, Optimize={Optimize}",
            watchPath,
            settings.Suffix,
            settings.Languages,
            settings.Deskew,
            settings.Clean,
            settings.Rotate,
            settings.Optimize);

        try
        {
            await watcherService.WatchAsync(watchPath, settings, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("KazoOCR Worker stopped gracefully");
        }
    }

    internal static string GetWatchPath() =>
        Environment.GetEnvironmentVariable(EnvWatchPath) ?? DefaultWatchPath;

    internal static OcrSettings BuildOcrSettings() => new()
    {
        Suffix = Environment.GetEnvironmentVariable(EnvSuffix) ?? DefaultSuffix,
        Languages = Environment.GetEnvironmentVariable(EnvLanguages) ?? DefaultLanguages,
        Deskew = ParseBool(Environment.GetEnvironmentVariable(EnvDeskew), DefaultDeskew),
        Clean = ParseBool(Environment.GetEnvironmentVariable(EnvClean), DefaultClean),
        Rotate = ParseBool(Environment.GetEnvironmentVariable(EnvRotate), DefaultRotate),
        Optimize = ParseInt(Environment.GetEnvironmentVariable(EnvOptimize), DefaultOptimize)
    };

    internal static bool ParseBool(string? value, bool defaultValue) =>
        bool.TryParse(value, out var result) ? result : defaultValue;

    internal static int ParseInt(string? value, int defaultValue) =>
        int.TryParse(value, out var result) ? result : defaultValue;
}
