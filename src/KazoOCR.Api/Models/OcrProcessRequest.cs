using System.ComponentModel.DataAnnotations;

namespace KazoOCR.Api.Models;

/// <summary>
/// Request model for OCR processing with optional settings.
/// </summary>
public sealed class OcrProcessRequest
{
    /// <summary>
    /// The suffix to append to processed files (e.g., "_OCR").
    /// </summary>
    /// <example>_OCR</example>
    public string? Suffix { get; set; }

    /// <summary>
    /// The OCR languages (e.g., "fra+eng").
    /// </summary>
    /// <example>fra+eng</example>
    public string? Languages { get; set; }

    /// <summary>
    /// Whether to apply deskewing.
    /// </summary>
    /// <example>true</example>
    public bool? Deskew { get; set; }

    /// <summary>
    /// Whether to apply cleaning via Unpaper.
    /// </summary>
    /// <example>false</example>
    public bool? Clean { get; set; }

    /// <summary>
    /// Whether to apply rotation correction.
    /// </summary>
    /// <example>true</example>
    public bool? Rotate { get; set; }

    /// <summary>
    /// The optimization level (0-3).
    /// </summary>
    /// <example>1</example>
    [Range(0, 3)]
    public int? Optimize { get; set; }
}
