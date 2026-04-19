using KazoOCR.Api.Models;

namespace KazoOCR.Api.Services;

/// <summary>
/// Interface for managing OCR job storage and retrieval.
/// </summary>
public interface IJobStore
{
    /// <summary>
    /// Creates a new job entry.
    /// </summary>
    /// <param name="inputFileName">The original input file name.</param>
    /// <returns>The created job result with a new unique ID.</returns>
    OcrJobResult CreateJob(string inputFileName);

    /// <summary>
    /// Updates an existing job's status.
    /// </summary>
    /// <param name="id">The job identifier.</param>
    /// <param name="status">The new status.</param>
    /// <param name="outputPath">The output path (for completed jobs).</param>
    /// <param name="errorMessage">The error message (for failed jobs).</param>
    /// <returns>The updated job result, or null if the job was not found.</returns>
    OcrJobResult? UpdateJob(string id, JobStatus status, string? outputPath = null, string? errorMessage = null);

    /// <summary>
    /// Gets a job by its identifier.
    /// </summary>
    /// <param name="id">The job identifier.</param>
    /// <returns>The job result, or null if not found.</returns>
    OcrJobResult? GetJob(string id);

    /// <summary>
    /// Gets all jobs.
    /// </summary>
    /// <returns>A collection of all job results.</returns>
    IEnumerable<OcrJobResult> GetAllJobs();

    /// <summary>
    /// Removes a job by its identifier.
    /// </summary>
    /// <param name="id">The job identifier.</param>
    /// <returns>True if the job was removed, false if it was not found.</returns>
    bool RemoveJob(string id);
}
