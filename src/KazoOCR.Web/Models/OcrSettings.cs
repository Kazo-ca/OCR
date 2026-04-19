namespace KazoOCR.Web.Models;

/// <summary>
/// Configuration settings for OCR processing (from API).
/// </summary>
public sealed class OcrSettings
{
    /// <summary>
    /// Gets or sets the suffix to append to processed files (e.g., "_OCR").
    /// </summary>
    public string Suffix { get; set; } = "_OCR";

    /// <summary>
    /// Gets or sets the OCR languages (e.g., "fra+eng").
    /// </summary>
    public string Languages { get; set; } = "fra+eng";

    /// <summary>
    /// Gets or sets a value indicating whether to apply deskewing.
    /// </summary>
    public bool Deskew { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to apply cleaning via Unpaper.
    /// </summary>
    public bool Clean { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to apply rotation correction.
    /// </summary>
    public bool Rotate { get; set; } = true;

    /// <summary>
    /// Gets or sets the optimization level (0-3).
    /// </summary>
    public int Optimize { get; set; } = 1;

    /// <summary>
    /// Gets or sets the watch path for automatic processing.
    /// </summary>
    public string WatchPath { get; set; } = "/data";
}
