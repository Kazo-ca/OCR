using KazoOCR.Api.Models;
using KazoOCR.Api.Services;
using KazoOCR.Core;
using Microsoft.AspNetCore.Mvc;

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

    private const string EnvSuffix = "KAZO_SUFFIX";
    private const string DefaultSuffix = "_OCR";

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
    /// <returns>The created job information.</returns>
    [HttpPost("process")]
    [ProducesResponseType(typeof(OcrJobResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessAsync(IFormFile file)
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

        // Save to temp file
        var tempFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(file.FileName)}";
        var tempPath = Path.Join(uploadDir, tempFileName);

        await using (var stream = new FileStream(tempPath, FileMode.Create))
        {
            await file.CopyToAsync(stream).ConfigureAwait(false);
        }

        // Create job
        var job = _jobService.CreateJob(file.FileName, tempPath);
        _logger.LogInformation("Created OCR job {JobId} for file {FileName}", job.Id, file.FileName);

        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
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
    public IActionResult DeleteJob(string id)
    {
        var removed = _jobService.RemoveJob(id);
        if (!removed)
        {
            return NotFound(new { error = "Job not found" });
        }

        _logger.LogInformation("Removed OCR job {JobId}", id);
        return NoContent();
    }
}
