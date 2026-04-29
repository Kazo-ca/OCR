namespace KazoOCR.Web.Models;

/// <summary>
/// Status of an OCR job.
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// Job is pending and waiting to be processed.
    /// </summary>
    Pending,

    /// <summary>
    /// Job is currently being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Job failed with an error.
    /// </summary>
    Failed
}
