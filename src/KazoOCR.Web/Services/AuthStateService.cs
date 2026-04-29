using KazoOCR.Web.Models;
using Microsoft.Extensions.Logging;

namespace KazoOCR.Web.Services;

/// <summary>
/// Service for managing authentication state in the browser.
/// </summary>
public sealed class AuthStateService
{
    private readonly IKazoApiClient _apiClient;
    private readonly ILogger<AuthStateService> _logger;
    private bool _isConfigured;
    private string? _token;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthStateService"/> class.
    /// </summary>
    /// <param name="apiClient">The API client.</param>
    /// <param name="logger">The logger.</param>
    public AuthStateService(IKazoApiClient apiClient, ILogger<AuthStateService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// Event raised when authentication state changes.
    /// </summary>
    public event Action? OnAuthStateChanged;

    /// <summary>
    /// Gets a value indicating whether the user is authenticated (has valid token).
    /// </summary>
    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    /// <summary>
    /// Gets a value indicating whether authentication is configured.
    /// </summary>
    public bool IsConfigured => _isConfigured;

    /// <summary>
    /// Gets the authentication token.
    /// </summary>
    public string? Token => _token;

    /// <summary>
    /// Refreshes the authentication status from the API.
    /// Note: This only updates the "configured" status, not authentication.
    /// Authentication is based on whether we have a valid token.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authentication status.</returns>
    public async Task<AuthStatus> RefreshStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var apiStatus = await _apiClient.GetAuthStatusAsync(cancellationToken).ConfigureAwait(false);
            _isConfigured = apiStatus.Configured;

            var status = new AuthStatus
            {
                Configured = _isConfigured,
                Authenticated = IsAuthenticated
            };

            OnAuthStateChanged?.Invoke();
            return status;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh auth status");
            // Keep existing configured status, just update authenticated based on token
            return new AuthStatus { Configured = _isConfigured, Authenticated = IsAuthenticated };
        }
    }

    /// <summary>
    /// Attempts to log in with the specified password.
    /// </summary>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if login was successful.</returns>
    public async Task<bool> LoginAsync(string password, CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.LoginAsync(
            new LoginRequest { Password = password },
            cancellationToken).ConfigureAwait(false);

        if (response.Success)
        {
            _token = response.Token;
            // Note: _isConfigured is not set here - it's managed by RefreshStatusAsync and SetupPasswordAsync
            OnAuthStateChanged?.Invoke();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        await _apiClient.LogoutAsync(cancellationToken).ConfigureAwait(false);
        _token = null;
        // Keep _isConfigured as true since the password is still set
        OnAuthStateChanged?.Invoke();
    }

    /// <summary>
    /// Creates the initial password during first-run setup.
    /// </summary>
    /// <param name="password">The password.</param>
    /// <param name="confirmPassword">The password confirmation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if setup was successful.</returns>
    public async Task<bool> SetupPasswordAsync(
        string password,
        string confirmPassword,
        CancellationToken cancellationToken = default)
    {
        if (password != confirmPassword)
        {
            return false;
        }

        var success = await _apiClient.SetupPasswordAsync(
            new SetupRequest { Password = password, ConfirmPassword = confirmPassword },
            cancellationToken).ConfigureAwait(false);

        if (success)
        {
            _isConfigured = true;
            // After setup, automatically log in
            return await LoginAsync(password, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }
}
