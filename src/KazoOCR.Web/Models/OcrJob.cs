namespace KazoOCR.Web.Models;

/// <summary>
/// Represents an OCR job returned by the API.
/// </summary>
public sealed class OcrJob
{
    /// <summary>
    /// Gets or sets the unique identifier for the job.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original filename.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the job status.
    /// </summary>
    public JobStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the job creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the job completion timestamp (if completed).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the output file path (if completed).
    /// </summary>
    public string? OutputPath { get; set; }
}
