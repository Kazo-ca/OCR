using System.Text.Json.Serialization;

namespace KazoOCR.Web.Models;

/// <summary>
/// Represents an OCR job returned by the API.
/// Matches KazoOCR.Api.Models.OcrJobResult record.
/// </summary>
public sealed class OcrJob
{
    /// <summary>
    /// Gets or sets the unique identifier for the job.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the original filename.
    /// API returns "inputFileName", this property provides backward compatibility.
    /// </summary>
    [JsonPropertyName("inputFileName")]
    public string InputFileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the filename (alias for InputFileName for backward compatibility).
    /// </summary>
    [JsonIgnore]
    public string FileName => InputFileName;

    /// <summary>
    /// Gets or sets the job status.
    /// </summary>
    [JsonPropertyName("status")]
    public JobStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the job creation timestamp.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the job completion timestamp (if completed).
    /// </summary>
    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the error message (if failed).
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the output file path (if completed).
    /// </summary>
    [JsonPropertyName("outputPath")]
    public string? OutputPath { get; set; }
}
