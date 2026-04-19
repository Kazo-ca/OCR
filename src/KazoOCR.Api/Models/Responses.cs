namespace KazoOCR.Api.Models;

/// <summary>
/// Response model when a job is successfully submitted.
/// </summary>
/// <param name="JobId">The unique identifier for the submitted job.</param>
/// <param name="Message">A human-readable message about the submission.</param>
public record OcrSubmitResponse(string JobId, string Message);

/// <summary>
/// Standard error response model.
/// </summary>
/// <param name="Error">A human-readable error message.</param>
/// <param name="Details">Additional details about the error (optional).</param>
public record ErrorResponse(string Error, string? Details = null);
