using KazoOCR.Api.Models;

namespace KazoOCR.Api.Services;

/// <summary>
/// Service for managing authentication state and operations
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Checks if a password has been configured (either via env var or setup endpoint)
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Sets up the initial password. Only works if no password is configured.
    /// </summary>
    /// <param name="password">The password to set</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if password was set, false if already configured</returns>
    Task<bool> SetupPasswordAsync(string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a password against the stored hash
    /// </summary>
    /// <param name="password">The password to validate</param>
    /// <returns>True if password is valid</returns>
    bool ValidatePassword(string password);

    /// <summary>
    /// Creates a new session token
    /// </summary>
    /// <returns>The token and its expiration time</returns>
    LoginResponse CreateToken();

    /// <summary>
    /// Validates a session token
    /// </summary>
    /// <param name="token">The token to validate</param>
    /// <returns>True if token is valid and not expired</returns>
    bool ValidateToken(string token);

    /// <summary>
    /// Invalidates a session token
    /// </summary>
    /// <param name="token">The token to invalidate</param>
    void InvalidateToken(string token);
}
