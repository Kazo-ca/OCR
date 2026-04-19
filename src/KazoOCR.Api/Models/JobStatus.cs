namespace KazoOCR.Api.Models;

/// <summary>
/// Represents the status of an OCR job.
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// The job is waiting to be processed.
    /// </summary>
    Pending,

    /// <summary>
    /// The job is currently being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// The job has completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// The job has failed.
    /// </summary>
    Failed
}
