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
    private AuthStatus? _cachedStatus;
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
    /// Gets a value indicating whether the user is authenticated.
    /// </summary>
    public bool IsAuthenticated => _cachedStatus?.Authenticated ?? false;

    /// <summary>
    /// Gets a value indicating whether authentication is configured.
    /// </summary>
    public bool IsConfigured => _cachedStatus?.Configured ?? false;

    /// <summary>
    /// Gets the authentication token.
    /// </summary>
    public string? Token => _token;

    /// <summary>
    /// Refreshes the authentication status from the API.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authentication status.</returns>
    public async Task<AuthStatus> RefreshStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _cachedStatus = await _apiClient.GetAuthStatusAsync(cancellationToken).ConfigureAwait(false);
            OnAuthStateChanged?.Invoke();
            return _cachedStatus;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh auth status");
            _cachedStatus = new AuthStatus { Configured = false, Authenticated = false };
            OnAuthStateChanged?.Invoke();
            return _cachedStatus;
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
            _cachedStatus = new AuthStatus { Configured = true, Authenticated = true };
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
        _cachedStatus = new AuthStatus { Configured = true, Authenticated = false };
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
            // After setup, automatically log in
            return await LoginAsync(password, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }
}
