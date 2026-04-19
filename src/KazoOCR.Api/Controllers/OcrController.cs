using System.Text.RegularExpressions;
using KazoOCR.Api.Models;
using KazoOCR.Api.Services;
using KazoOCR.Core;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KazoOCR.Api.Controllers;

/// <summary>
/// Controller for OCR processing operations.
/// </summary>
[ApiController]
[Route("api/ocr")]
[Produces("application/json")]
public partial class OcrController(
    IJobStore jobStore,
    IOcrFileService fileService,
    IOcrProcessRunner processRunner,
    ILogger<OcrController> logger) : ControllerBase
{
    private static readonly string[] AllowedExtensions = [".pdf"];

    // Regex to validate job ID format (alphanumeric only, 32 chars for GUID without dashes)
    [GeneratedRegex(@"^[a-zA-Z0-9]{1,64}$")]
    private static partial Regex JobIdRegex();

    /// <summary>
    /// Sanitizes a job ID for safe logging to prevent log forging attacks.
    /// </summary>
    private static string SanitizeForLogging(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "[empty]";
        }

        // Remove or replace control characters and newlines
        return value.Replace("\r", "").Replace("\n", " ").Replace("\t", " ");
    }

    /// <summary>
    /// Validates that the job ID matches expected format.
    /// </summary>
    private static bool IsValidJobId(string? id) =>
        !string.IsNullOrEmpty(id) && JobIdRegex().IsMatch(id);

    /// <summary>
    /// Submit a PDF file for OCR processing.
    /// </summary>
    /// <param name="file">The PDF file to process (multipart/form-data).</param>
    /// <param name="request">Optional OCR settings to override defaults.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A job identifier for tracking the processing status.</returns>
    /// <response code="202">Job accepted and queued for processing.</response>
    /// <response code="400">Invalid file or request parameters.</response>
    [HttpPost("process")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "Submit a PDF for OCR processing",
        Description = "Accepts a PDF file via multipart/form-data and queues it for OCR processing. Returns a job ID to track progress.")]
    [ProducesResponseType(typeof(OcrSubmitResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessFile(
        IFormFile file,
        [FromForm] OcrProcessRequest? request,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ErrorResponse("No file uploaded", "Please provide a PDF file."));
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(new ErrorResponse("Invalid file type", "Only PDF files are supported."));
        }

        // Create job entry
        var job = jobStore.CreateJob(file.FileName);
        logger.LogInformation("Created job {JobId} for file {FileName}", job.Id, file.FileName);

        // Save the file temporarily and process in background
        // Use application-specific temp directory to avoid conflicts
        var tempDir = Path.Combine(Path.GetTempPath(), "KazoOCR");
        Directory.CreateDirectory(tempDir);
        var tempPath = Path.Combine(tempDir, $"{job.Id}{extension}");
        await using (var stream = new FileStream(tempPath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        // Build OCR settings
        var settings = new OcrSettings
        {
            Suffix = request?.Suffix ?? "_OCR",
            Languages = request?.Languages ?? "fra+eng",
            Deskew = request?.Deskew ?? true,
            Clean = request?.Clean ?? false,
            Rotate = request?.Rotate ?? true,
            Optimize = request?.Optimize ?? 1
        };

        // Start background processing
        _ = Task.Run(async () =>
        {
            try
            {
                jobStore.UpdateJob(job.Id, JobStatus.Processing);
                var outputPath = fileService.ComputeOutputPath(tempPath, settings.Suffix);

                var result = await processRunner.RunAsync(settings, tempPath, outputPath, CancellationToken.None);

                if (result.ExitCode == 0)
                {
                    jobStore.UpdateJob(job.Id, JobStatus.Completed, outputPath);
                    logger.LogInformation("Job {JobId} completed successfully", job.Id);
                }
                else
                {
                    // Sanitize error message for logging to prevent log forging
                    var sanitizedError = result.StandardError?.Replace("\r", "").Replace("\n", " ") ?? "Unknown error";
                    jobStore.UpdateJob(job.Id, JobStatus.Failed, errorMessage: sanitizedError);
                    logger.LogWarning("Job {JobId} failed with exit code {ExitCode}", job.Id, result.ExitCode);
                }
            }
            catch (Exception ex) when (IsNonFatal(ex))
            {
                // Don't expose internal exception details to users - use a generic message
                // The full exception is logged below for debugging
                jobStore.UpdateJob(job.Id, JobStatus.Failed, errorMessage: "An internal error occurred during processing.");
                logger.LogError(ex, "Job {JobId} failed with exception", job.Id);
            }
            finally
            {
                // Clean up temporary input file
                try
                {
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                        logger.LogDebug("Cleaned up temporary file for job {JobId}", job.Id);
                    }
                }
                catch (IOException ex)
                {
                    logger.LogWarning(ex, "Failed to clean up temporary file for job {JobId}", job.Id);
                }
            }
        }, CancellationToken.None);

    private static bool IsNonFatal(Exception ex) =>
        ex is not OutOfMemoryException
        and not StackOverflowException
        and not AccessViolationException
        and not AppDomainUnloadedException
        and not BadImageFormatException
        and not CannotUnloadAppDomainException
        and not InvalidProgramException;

        return AcceptedAtAction(
            nameof(GetJob),
            new { id = job.Id },
            new OcrSubmitResponse(job.Id, "Job accepted and queued for processing."));
    }

    /// <summary>
    /// Get all OCR jobs.
    /// </summary>
    /// <returns>A list of all jobs with their current status.</returns>
    /// <response code="200">Returns the list of jobs.</response>
    [HttpGet("jobs")]
    [SwaggerOperation(
        Summary = "List all OCR jobs",
        Description = "Returns all jobs with their current status, ordered by creation date (newest first).")]
    [ProducesResponseType(typeof(IEnumerable<OcrJobResult>), StatusCodes.Status200OK)]
    public IActionResult GetJobs()
    {
        return Ok(jobStore.GetAllJobs());
    }

    /// <summary>
    /// Get a specific OCR job by ID.
    /// </summary>
    /// <param name="id">The job identifier.</param>
    /// <returns>The job details with current status.</returns>
    /// <response code="200">Returns the job details.</response>
    /// <response code="400">Invalid job ID format.</response>
    /// <response code="404">Job not found.</response>
    [HttpGet("jobs/{id}")]
    [SwaggerOperation(
        Summary = "Get OCR job status",
        Description = "Returns the current status and details of a specific OCR job.")]
    [ProducesResponseType(typeof(OcrJobResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetJob(string id)
    {
        if (!IsValidJobId(id))
        {
            return BadRequest(new ErrorResponse("Invalid job ID format"));
        }

        var job = jobStore.GetJob(id);
        if (job is null)
        {
            return NotFound(new ErrorResponse("Job not found", $"No job found with ID: {SanitizeForLogging(id)}"));
        }

        return Ok(job);
    }

    /// <summary>
    /// Cancel or remove an OCR job.
    /// </summary>
    /// <param name="id">The job identifier.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Job successfully removed.</response>
    /// <response code="400">Invalid job ID format.</response>
    /// <response code="404">Job not found.</response>
    [HttpDelete("jobs/{id}")]
    [SwaggerOperation(
        Summary = "Cancel or remove an OCR job",
        Description = "Removes a job from the queue. If the job is currently processing, it will be cancelled.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult DeleteJob(string id)
    {
        if (!IsValidJobId(id))
        {
            return BadRequest(new ErrorResponse("Invalid job ID format"));
        }

        var removed = jobStore.RemoveJob(id);
        if (!removed)
        {
            return NotFound(new ErrorResponse("Job not found", $"No job found with ID: {SanitizeForLogging(id)}"));
        }

        // Since id is validated above, it's safe to log directly, but we use sanitized version for defense in depth
        logger.LogInformation("Job {JobId} removed", SanitizeForLogging(id));
        return NoContent();
    }
}
