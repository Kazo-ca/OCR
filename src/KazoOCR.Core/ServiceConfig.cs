namespace KazoOCR.Core;

/// <summary>
/// Configuration for a single watch folder.
/// </summary>
public sealed class WatchFolderConfig
{
    /// <summary>
    /// Gets or sets the path to watch.
    /// </summary>
    public string Path { get; set; } = string.Empty;

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
    /// Converts this configuration to an <see cref="OcrSettings"/> instance.
    /// </summary>
    /// <returns>An <see cref="OcrSettings"/> with the same values.</returns>
    public OcrSettings ToOcrSettings() => new()
    {
        Suffix = Suffix,
        Languages = Languages,
        Deskew = Deskew,
        Clean = Clean,
        Rotate = Rotate,
        Optimize = Optimize
    };
}

/// <summary>
/// Configuration for the KazoOCR service.
/// </summary>
public sealed class ServiceConfig
{
    /// <summary>
    /// Gets or sets the list of folders to watch.
    /// </summary>
    public List<WatchFolderConfig> WatchFolders { get; set; } = [];
}
