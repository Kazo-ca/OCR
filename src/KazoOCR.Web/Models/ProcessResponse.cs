namespace KazoOCR.Web.Models;

/// <summary>
/// Response from submitting an OCR job.
/// </summary>
public sealed class ProcessResponse
{
    /// <summary>
    /// Gets or sets the job ID.
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether submission was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message (if failed).
    /// </summary>
    public string? Error { get; set; }
}
