using KazoOCR.Api.Models;

namespace KazoOCR.Api.Services;

/// <summary>
/// Interface for OCR job management.
/// </summary>
public interface IOcrJobService
{
    /// <summary>
    /// Creates a new OCR job.
    /// </summary>
    /// <param name="inputFileName">The original file name.</param>
    /// <param name="inputPath">The temporary path where the input file is stored.</param>
    /// <returns>The created job result.</returns>
    OcrJobResult CreateJob(string inputFileName, string inputPath);

    /// <summary>
    /// Gets a job by its identifier.
    /// </summary>
    /// <param name="id">The job identifier.</param>
    /// <returns>The job result, or null if not found.</returns>
    OcrJobResult? GetJob(string id);

    /// <summary>
    /// Gets all jobs.
    /// </summary>
    /// <returns>A list of all job results.</returns>
    IReadOnlyList<OcrJobResult> GetAllJobs();

    /// <summary>
    /// Updates a job's status to processing.
    /// </summary>
    /// <param name="id">The job identifier.</param>
    /// <returns>True if the job was updated, false if not found.</returns>
    bool StartProcessing(string id);

    /// <summary>
    /// Marks a job as completed.
    /// </summary>
    /// <param name="id">The job identifier.</param>
    /// <param name="outputPath">The output file path.</param>
    /// <returns>True if the job was updated, false if not found.</returns>
    bool MarkCompleted(string id, string outputPath);

    /// <summary>
    /// Marks a job as failed.
    /// </summary>
    /// <param name="id">The job identifier.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>True if the job was updated, false if not found.</returns>
    bool MarkFailed(string id, string errorMessage);

    /// <summary>
    /// Removes a job.
    /// </summary>
    /// <param name="id">The job identifier.</param>
    /// <returns>True if the job was removed, false if not found.</returns>
    bool RemoveJob(string id);

    /// <summary>
    /// Gets the next pending job.
    /// </summary>
    /// <returns>The next pending job, or null if none available.</returns>
    OcrJobResult? GetNextPendingJob();
}
