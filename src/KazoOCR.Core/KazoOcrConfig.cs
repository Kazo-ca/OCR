namespace KazoOCR.Core;

/// <summary>
/// Persistent application configuration for KazoOCR.
/// Stored in %APPDATA%\KazoOCR\config.json on Windows.
/// </summary>
public sealed class KazoOcrConfig
{
    /// <summary>
    /// Gets or sets the WSL distribution to use for OCR processing.
    /// <c>null</c> means no distro has been selected yet (triggers first-run prompt).
    /// </summary>
    public string? WslDistro { get; set; }
}
