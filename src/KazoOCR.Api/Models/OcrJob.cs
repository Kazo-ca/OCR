namespace KazoOCR.Api.Models;

/// <summary>
/// Represents an internal OCR job with mutable state.
/// </summary>
internal sealed class OcrJob
{
    /// <summary>
    /// Gets the unique identifier of the job.
    /// </summary>
    public string Id { get; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the original input file name.
    /// </summary>
    public required string InputFileName { get; init; }

    /// <summary>
    /// Gets or sets the temporary input file path.
    /// </summary>
    public required string InputPath { get; init; }

    /// <summary>
    /// Gets or sets the expected output file path.
    /// </summary>
    public string? OutputPath { get; set; }

    /// <summary>
    /// Gets or sets the current status of the job.
    /// </summary>
    public JobStatus Status { get; set; } = JobStatus.Pending;

    /// <summary>
    /// Gets or sets the error message if the job failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets the timestamp when the job was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when the job completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Converts this job to a result record.
    /// </summary>
    /// <returns>An <see cref="OcrJobResult"/> representing this job.</returns>
    public OcrJobResult ToResult() => new(
        Id,
        InputFileName,
        Status,
        OutputPath,
        ErrorMessage,
        CreatedAt,
        CompletedAt
    );
}
