namespace KazoOCR.Api.Models;

/// <summary>
/// Represents the result of an OCR job.
/// </summary>
/// <param name="Id">The unique identifier of the job.</param>
/// <param name="InputFileName">The original input file name.</param>
/// <param name="Status">The current status of the job.</param>
/// <param name="OutputPath">The output path if completed successfully.</param>
/// <param name="ErrorMessage">The error message if the job failed.</param>
/// <param name="CreatedAt">The timestamp when the job was created.</param>
/// <param name="CompletedAt">The timestamp when the job completed.</param>
public record OcrJobResult(
    string Id,
    string InputFileName,
    JobStatus Status,
    string? OutputPath,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt
);
