using KazoOCR.Web.Models;

namespace KazoOCR.Web.Services;

/// <summary>
/// Service interface for communicating with KazoOCR API.
/// </summary>
public interface IKazoApiClient
{
    /// <summary>
    /// Gets the authentication status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authentication status.</returns>
    Task<AuthStatus> GetAuthStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs in with the specified credentials.
    /// </summary>
    /// <param name="request">Login request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Login response.</returns>
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the initial password during first-run setup.
    /// </summary>
    /// <param name="request">Setup request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> SetupPasswordAsync(SetupRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all OCR jobs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of OCR jobs.</returns>
    Task<IReadOnlyList<OcrJob>> GetJobsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific OCR job by ID.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The OCR job, or null if not found.</returns>
    Task<OcrJob?> GetJobAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a PDF file for OCR processing.
    /// </summary>
    /// <param name="fileName">Original file name.</param>
    /// <param name="fileContent">File content stream.</param>
    /// <param name="options">OCR options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Process response with job ID.</returns>
    Task<ProcessResponse> ProcessFileAsync(
        string fileName,
        Stream fileContent,
        OcrOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels/removes an OCR job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> DeleteJobAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current OCR settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>OCR settings.</returns>
    Task<OcrSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the OCR settings.
    /// </summary>
    /// <param name="settings">New settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> UpdateSettingsAsync(OcrSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the output file for a completed job.
    /// </summary>
    /// <param name="jobId">Job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>File content stream, or null if not available.</returns>
    Task<Stream?> DownloadOutputAsync(string jobId, CancellationToken cancellationToken = default);
}
