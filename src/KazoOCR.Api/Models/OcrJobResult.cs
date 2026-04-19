namespace KazoOCR.Api.Models;

/// <summary>
/// Represents the result of an OCR processing job.
/// </summary>
/// <param name="Id">The unique identifier for this job.</param>
/// <param name="InputFileName">The original input file name.</param>
/// <param name="Status">The current status of the job.</param>
/// <param name="OutputPath">The path to the processed file (null if not completed).</param>
/// <param name="ErrorMessage">Error message if the job failed (null otherwise).</param>
/// <param name="CreatedAt">When the job was created.</param>
/// <param name="CompletedAt">When the job completed (null if not completed).</param>
public record OcrJobResult(
    string Id,
    string InputFileName,
    JobStatus Status,
    string? OutputPath,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
