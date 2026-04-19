namespace KazoOCR.Web.Models;

/// <summary>
/// OCR processing options for job submission.
/// </summary>
public sealed class OcrOptions
{
    /// <summary>
    /// Gets or sets the OCR languages (e.g., "fra+eng").
    /// </summary>
    public string Languages { get; set; } = "fra+eng";

    /// <summary>
    /// Gets or sets a value indicating whether to apply deskewing.
    /// </summary>
    public bool Deskew { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to apply cleaning via Unpaper.
    /// </summary>
    public bool Clean { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to apply rotation correction.
    /// </summary>
    public bool Rotate { get; set; }

    /// <summary>
    /// Gets or sets the optimization level (0-3).
    /// </summary>
    public int Optimize { get; set; } = 1;
}
