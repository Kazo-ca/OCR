using System.Net.Http.Json;
using System.Text.Json;
using KazoOCR.Web.Models;
using Microsoft.Extensions.Logging;

namespace KazoOCR.Web.Services;

/// <summary>
/// Implementation of the API client for communicating with KazoOCR API.
/// </summary>
public sealed class KazoApiClient : IKazoApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<KazoApiClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KazoApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public KazoApiClient(HttpClient httpClient, ILogger<KazoApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuthStatus> GetAuthStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<AuthStatus>(
                "api/auth/status",
                cancellationToken).ConfigureAwait(false);

            return response ?? new AuthStatus { Configured = false };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to get auth status");
            return new AuthStatus { Configured = false };
        }
    }

    /// <inheritdoc />
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/login",
                request,
                cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken).ConfigureAwait(false);
                return result ?? new LoginResponse();
            }

            // Try to extract error from response body
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning("Login failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
            return new LoginResponse();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Login request failed");
            return new LoginResponse();
        }
    }

    /// <inheritdoc />
    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsync("api/auth/logout", null, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Logout request failed");
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetupPasswordAsync(SetupRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/setup",
                new { password = request.Password },
                cancellationToken).ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Setup password request failed");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OcrJob>> GetJobsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var jobs = await _httpClient.GetFromJsonAsync<List<OcrJob>>(
                "api/ocr/jobs",
                cancellationToken).ConfigureAwait(false);

            return jobs ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to get jobs");
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<OcrJob?> GetJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<OcrJob>(
                $"api/ocr/jobs/{Uri.EscapeDataString(jobId)}",
                cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to get job {JobId}", jobId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ProcessResponse> ProcessFileAsync(
        string fileName,
        Stream fileContent,
        OcrOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        // NOTE: The API currently only accepts the file parameter.
        // OCR options (languages, deskew, clean, rotate, optimize) are not yet
        // supported by the API and will be ignored until API support is added.

        try
        {
            using var content = new MultipartFormDataContent();
            using var fileStreamContent = new StreamContent(fileContent);

            content.Add(fileStreamContent, "file", fileName);

            using var response = await _httpClient.PostAsync(
                "api/ocr/process",
                content,
                cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                // API returns OcrJobResult on success (201 Created)
                var result = await response.Content.ReadFromJsonAsync<ProcessResponse>(cancellationToken).ConfigureAwait(false);
                if (result != null)
                {
                    result.Success = true;
                    return result;
                }

                return new ProcessResponse { Success = false, Error = "Invalid response from server" };
            }

            // Try to extract error from response body
            try
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var errorDoc = JsonDocument.Parse(errorContent);
                if (errorDoc.RootElement.TryGetProperty("error", out var errorProp))
                {
                    return new ProcessResponse { Success = false, Error = errorProp.GetString() };
                }
            }
            catch (JsonException)
            {
                // Ignore JSON parse errors
            }

            return new ProcessResponse { Success = false, Error = $"Processing failed: {response.ReasonPhrase}" };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Process file request failed");
            return new ProcessResponse { Success = false, Error = "Connection error" };
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.DeleteAsync(
                $"api/ocr/jobs/{Uri.EscapeDataString(jobId)}",
                cancellationToken).ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Delete job request failed for {JobId}", jobId);
            return false;
        }
    }

    /// <inheritdoc />
    public Task<OcrSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        // NOTE: Settings endpoint not yet implemented in API
        // Return default settings for now
        _logger.LogDebug("Settings endpoint not implemented - returning defaults");
        return Task.FromResult(new OcrSettings());
    }

    /// <inheritdoc />
    public Task<bool> UpdateSettingsAsync(OcrSettings settings, CancellationToken cancellationToken = default)
    {
        // NOTE: Settings endpoint not yet implemented in API
        _logger.LogWarning("Settings endpoint not implemented - settings not saved");
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<Stream?> DownloadOutputAsync(string jobId, CancellationToken cancellationToken = default)
    {
        // NOTE: Download endpoint not yet implemented in API
        _logger.LogWarning("Download endpoint not implemented for job {JobId}", jobId);
        return Task.FromResult<Stream?>(null);
    }
}
