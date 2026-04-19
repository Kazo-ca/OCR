using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using KazoOCR.Api.Models;

namespace KazoOCR.Api.Services;

/// <summary>
/// Implementation of authentication service with bcrypt password hashing
/// and in-memory session token management
/// </summary>
public class AuthService : IAuthService
{
    private readonly string _authFilePath;
    private readonly TimeSpan _tokenExpiration;
    private readonly ILogger<AuthService> _logger;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _activeSessions = new();
    private readonly object _fileLock = new();

    private string? _passwordHash;
    private bool _isInitialized;

    public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        
        _logger = logger;

        // Get data directory from configuration or default to "data"
        var dataPath = configuration["KAZO_DATA_PATH"] ?? "data";
        _authFilePath = Path.Join(dataPath, "auth.json");

        // Session token expiration (default 24 hours)
        var expirationHours = configuration.GetValue<int?>("KAZO_SESSION_EXPIRATION_HOURS") ?? 24;
        _tokenExpiration = TimeSpan.FromHours(expirationHours);

        // Initialize password from env var or file
        Initialize(configuration);
    }

    private void Initialize(IConfiguration configuration)
    {
        if (_isInitialized)
            return;

        // Check for KAZO_DEFAULT_PASSWORD env var
        var defaultPassword = configuration["KAZO_DEFAULT_PASSWORD"];
        if (!string.IsNullOrEmpty(defaultPassword))
        {
            // If env var is set and no existing hash, hash and store it
            if (!TryLoadPasswordHash())
            {
                _passwordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);
                SavePasswordHash(_passwordHash);
                _logger.LogInformation("Password initialized from KAZO_DEFAULT_PASSWORD environment variable");
            }
            else
            {
                _logger.LogInformation("Password already configured, ignoring KAZO_DEFAULT_PASSWORD");
            }
        }
        else
        {
            // Try to load existing password hash from file
            TryLoadPasswordHash();
        }

        _isInitialized = true;
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_passwordHash);

    public async Task<bool> SetupPasswordAsync(string password, CancellationToken cancellationToken = default)
    {
        if (IsConfigured)
        {
            _logger.LogWarning("Attempted to setup password when already configured");
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be empty", nameof(password));
        }

        // Hash the password using bcrypt with automatic salt generation
        var hash = await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(password), cancellationToken);
        
        _passwordHash = hash;
        SavePasswordHash(hash);
        
        _logger.LogInformation("Password configured via setup endpoint");
        return true;
    }

    public bool ValidatePassword(string password)
    {
        if (!IsConfigured || string.IsNullOrEmpty(password))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, _passwordHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password");
            return false;
        }
    }

    public LoginResponse CreateToken()
    {
        // Generate a cryptographically secure random token
        var tokenBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }
        var token = Convert.ToBase64String(tokenBytes);

        var expiresAt = DateTimeOffset.UtcNow.Add(_tokenExpiration);
        _activeSessions[token] = expiresAt;

        _logger.LogDebug("Created new session token expiring at {ExpiresAt}", expiresAt);

        // Clean up expired sessions periodically
        CleanupExpiredSessions();

        return new LoginResponse(token, expiresAt);
    }

    public bool ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        if (_activeSessions.TryGetValue(token, out var expiresAt))
        {
            if (DateTimeOffset.UtcNow < expiresAt)
            {
                return true;
            }

            // Token expired, remove it
            _activeSessions.TryRemove(token, out _);
        }

        return false;
    }

    public void InvalidateToken(string token)
    {
        if (!string.IsNullOrEmpty(token))
        {
            _activeSessions.TryRemove(token, out _);
            _logger.LogDebug("Session token invalidated");
        }
    }

    private bool TryLoadPasswordHash()
    {
        lock (_fileLock)
        {
            if (!File.Exists(_authFilePath))
                return false;

            try
            {
                var json = File.ReadAllText(_authFilePath);
                var authData = JsonSerializer.Deserialize<AuthData>(json);
                
                if (authData != null && !string.IsNullOrEmpty(authData.PasswordHash))
                {
                    _passwordHash = authData.PasswordHash;
                    _logger.LogInformation("Loaded password hash from {AuthFilePath}", _authFilePath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading auth data from {AuthFilePath}", _authFilePath);
            }

            return false;
        }
    }

    private void SavePasswordHash(string hash)
    {
        lock (_fileLock)
        {
            try
            {
                var directory = Path.GetDirectoryName(_authFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var authData = new AuthData(hash);
                var json = JsonSerializer.Serialize(authData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_authFilePath, json);
                
                _logger.LogInformation("Saved password hash to {AuthFilePath}", _authFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving auth data to {AuthFilePath}", _authFilePath);
                throw;
            }
        }
    }

    private void CleanupExpiredSessions()
    {
        var now = DateTimeOffset.UtcNow;
        var expiredTokens = _activeSessions.Where(kvp => kvp.Value <= now).Select(kvp => kvp.Key).ToList();
        
        foreach (var token in expiredTokens)
        {
            _activeSessions.TryRemove(token, out _);
        }

        if (expiredTokens.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} expired session tokens", expiredTokens.Count);
        }
    }
}
