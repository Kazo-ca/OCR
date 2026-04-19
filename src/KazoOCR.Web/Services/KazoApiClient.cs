using System.Net.Http.Json;
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

            return response ?? new AuthStatus { Configured = false, Authenticated = false };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to get auth status");
            return new AuthStatus { Configured = false, Authenticated = false };
        }
    }

    /// <inheritdoc />
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/auth/login",
                request,
                cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken).ConfigureAwait(false);
                return result ?? new LoginResponse { Success = false, Error = "Invalid response" };
            }

            return new LoginResponse { Success = false, Error = $"Login failed: {response.ReasonPhrase}" };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Login request failed");
            return new LoginResponse { Success = false, Error = "Connection error" };
        }
    }

    /// <inheritdoc />
    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClient.PostAsync("api/auth/logout", null, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Logout request failed");
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetupPasswordAsync(SetupRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/auth/setup",
                request,
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

        try
        {
            using var content = new MultipartFormDataContent();
            using var fileStream = new StreamContent(fileContent);
            using var languagesContent = new StringContent(options.Languages);
            using var deskewContent = new StringContent(options.Deskew.ToString().ToLowerInvariant());
            using var cleanContent = new StringContent(options.Clean.ToString().ToLowerInvariant());
            using var rotateContent = new StringContent(options.Rotate.ToString().ToLowerInvariant());
            using var optimizeContent = new StringContent(options.Optimize.ToString());

            content.Add(fileStream, "file", fileName);
            content.Add(languagesContent, "languages");
            content.Add(deskewContent, "deskew");
            content.Add(cleanContent, "clean");
            content.Add(rotateContent, "rotate");
            content.Add(optimizeContent, "optimize");

            var response = await _httpClient.PostAsync(
                "api/ocr/process",
                content,
                cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ProcessResponse>(cancellationToken).ConfigureAwait(false);
                return result ?? new ProcessResponse { Success = false, Error = "Invalid response" };
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
            var response = await _httpClient.DeleteAsync(
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
    public async Task<OcrSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _httpClient.GetFromJsonAsync<OcrSettings>(
                "api/settings",
                cancellationToken).ConfigureAwait(false);

            return settings ?? new OcrSettings();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to get settings");
            return new OcrSettings();
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateSettingsAsync(OcrSettings settings, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                "api/settings",
                settings,
                cancellationToken).ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Update settings request failed");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Stream?> DownloadOutputAsync(string jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/ocr/jobs/{Uri.EscapeDataString(jobId)}/download",
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            }

            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Download output request failed for {JobId}", jobId);
            return null;
        }
    }
}
