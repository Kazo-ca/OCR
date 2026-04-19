using KazoOCR.Api.Models;
using KazoOCR.Api.Services;
using KazoOCR.Core;
using Microsoft.AspNetCore.Mvc;
using static KazoOCR.Api.ApiConfiguration;

namespace KazoOCR.Api.Controllers;

/// <summary>
/// Controller for OCR operations.
/// </summary>
[ApiController]
[Route("api/ocr")]
public sealed class OcrController : ControllerBase
{
    private readonly IOcrJobService _jobService;
    private readonly IOcrFileService _fileService;
    private readonly ILogger<OcrController> _logger;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="OcrController"/> class.
    /// </summary>
    public OcrController(
        IOcrJobService jobService,
        IOcrFileService fileService,
        ILogger<OcrController> logger,
        IConfiguration configuration)
    {
        _jobService = jobService;
        _fileService = fileService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Submit a PDF for OCR processing.
    /// </summary>
    /// <param name="file">The PDF file to process.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created job information.</returns>
    [HttpPost("process")]
    [ProducesResponseType(typeof(OcrJobResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Only PDF files are accepted" });
        }

        // Validate with file service
        var suffix = _configuration[EnvSuffix]
            ?? Environment.GetEnvironmentVariable(EnvSuffix)
            ?? DefaultSuffix;

        if (_fileService.IsAlreadyProcessed(file.FileName, suffix))
        {
            return BadRequest(new { error = "File appears to already be processed (contains OCR suffix)" });
        }

        // Create temp directory for uploads
        var uploadDir = Path.Join(Path.GetTempPath(), "kazoocr-uploads");
        Directory.CreateDirectory(uploadDir);

        // Save to temp file - use Path.GetFileName to sanitize against path traversal
        var safeFileName = Path.GetFileName(file.FileName);
        var tempFileName = $"{Guid.NewGuid():N}_{safeFileName}";
        var tempPath = Path.Join(uploadDir, tempFileName);

        var jobCreated = false;

        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
            }

            // Create job
            var job = _jobService.CreateJob(safeFileName, tempPath);
            jobCreated = true;
            _logger.LogInformation("Created OCR job {JobId}", job.Id);

            return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
        }
        catch
        {
            if (!jobCreated && System.IO.File.Exists(tempPath))
            {
                try
                {
                    System.IO.File.Delete(tempPath);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(
                        cleanupEx,
                        "Failed to delete temporary uploaded file after OCR job submission failure");
                }
            }

            throw;
        }
    }

    /// <summary>
    /// Get all OCR jobs.
    /// </summary>
    /// <returns>A list of all jobs.</returns>
    [HttpGet("jobs")]
    [ProducesResponseType(typeof(IReadOnlyList<OcrJobResult>), StatusCodes.Status200OK)]
    public IActionResult GetJobs()
    {
        var jobs = _jobService.GetAllJobs();
        return Ok(jobs);
    }

    /// <summary>
    /// Get a specific OCR job by ID.
    /// </summary>
    /// <param name="id">The job identifier.</param>
    /// <returns>The job information.</returns>
    [HttpGet("jobs/{id}")]
    [ProducesResponseType(typeof(OcrJobResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetJob(string id)
    {
        // Validate job ID format (hex string of 32 chars)
        if (!IsValidJobId(id))
        {
            return NotFound(new { error = "Job not found" });
        }

        var job = _jobService.GetJob(id);
        if (job is null)
        {
            return NotFound(new { error = "Job not found" });
        }

        return Ok(job);
    }

    /// <summary>
    /// Cancel or remove an OCR job.
    /// </summary>
    /// <param name="id">The job identifier.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("jobs/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult DeleteJob(string id)
    {
        // Validate job ID format (hex string of 32 chars)
        if (!IsValidJobId(id))
        {
            return NotFound(new { error = "Job not found" });
        }

        // Check if job exists and is not currently processing
        var job = _jobService.GetJob(id);
        if (job is null)
        {
            return NotFound(new { error = "Job not found" });
        }

        if (job.Status == Models.JobStatus.Processing)
        {
            return Conflict(new { error = "Cannot delete a job that is currently processing" });
        }

        var removed = _jobService.RemoveJob(id);
        if (!removed)
        {
            return NotFound(new { error = "Job not found" });
        }

        _logger.LogInformation("Removed OCR job {JobId}", id);
        return NoContent();
    }

    /// <summary>
    /// Validates that a job ID is in the expected format (32-char hex string).
    /// </summary>
    private static bool IsValidJobId(string id) =>
        !string.IsNullOrEmpty(id) &&
        id.Length == 32 &&
        id.All(c => char.IsAsciiHexDigit(c));
}
