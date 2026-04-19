using KazoOCR.Core;

namespace KazoOCR.Api;

/// <summary>
/// Shared configuration constants and defaults for the KazoOCR API.
/// </summary>
internal static class ApiConfiguration
{
    // Environment variable names
    public const string EnvApiPort = "KAZO_API_PORT";
    public const string EnvWatchPath = "KAZO_WATCH_PATH";
    public const string EnvSuffix = "KAZO_SUFFIX";
    public const string EnvLanguages = "KAZO_LANGUAGES";
    public const string EnvDeskew = "KAZO_DESKEW";
    public const string EnvClean = "KAZO_CLEAN";
    public const string EnvRotate = "KAZO_ROTATE";
    public const string EnvOptimize = "KAZO_OPTIMIZE";

    // Default values
    public const string DefaultApiPort = "5000";
    public const string DefaultWatchPath = "/data";
    public const string DefaultSuffix = "_OCR";
    public const string DefaultLanguages = "fra+eng";
    public const bool DefaultDeskew = true;
    public const bool DefaultClean = false;
    public const bool DefaultRotate = true;
    public const int DefaultOptimize = 1;

    /// <summary>
    /// Parses a boolean value from a string, returning a default if parsing fails.
    /// </summary>
    public static bool ParseBool(string? value, bool defaultValue) =>
        bool.TryParse(value, out var result) ? result : defaultValue;

    /// <summary>
    /// Parses an integer value from a string, returning a default if parsing fails.
    /// </summary>
    public static int ParseInt(string? value, int defaultValue) =>
        int.TryParse(value, out var result) ? result : defaultValue;

    /// <summary>
    /// Gets a configuration value from IConfiguration or environment variables.
    /// </summary>
    public static string GetConfigValue(IConfiguration configuration, string key, string? defaultValue) =>
        configuration[key]
        ?? Environment.GetEnvironmentVariable(key)
        ?? defaultValue
        ?? string.Empty;

    /// <summary>
    /// Builds OCR settings from configuration.
    /// </summary>
    public static OcrSettings BuildOcrSettings(IConfiguration configuration) => new()
    {
        Suffix = GetConfigValue(configuration, EnvSuffix, DefaultSuffix),
        Languages = GetConfigValue(configuration, EnvLanguages, DefaultLanguages),
        Deskew = ParseBool(GetConfigValue(configuration, EnvDeskew, null), DefaultDeskew),
        Clean = ParseBool(GetConfigValue(configuration, EnvClean, null), DefaultClean),
        Rotate = ParseBool(GetConfigValue(configuration, EnvRotate, null), DefaultRotate),
        Optimize = ParseInt(GetConfigValue(configuration, EnvOptimize, null), DefaultOptimize)
    };
}
