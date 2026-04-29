using System.Text.Json.Serialization;

namespace KazoOCR.Web.Models;

/// <summary>
/// Response from submitting an OCR job.
/// The API returns an OcrJobResult on success (201 Created).
/// This model wraps the response for easier client handling.
/// </summary>
public sealed class ProcessResponse
{
    /// <summary>
    /// Gets or sets the job ID.
    /// Mapped from OcrJobResult.Id on success.
    /// </summary>
    [JsonPropertyName("id")]
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether submission was successful.
    /// </summary>
    [JsonIgnore]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message (if failed).
    /// </summary>
    [JsonIgnore]
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the input filename from the created job.
    /// </summary>
    [JsonPropertyName("inputFileName")]
    public string? InputFileName { get; set; }

    /// <summary>
    /// Gets or sets the job status.
    /// </summary>
    [JsonPropertyName("status")]
    public JobStatus Status { get; set; }
}
