namespace KazoOCR.Api.Models;

/// <summary>
/// Response for GET /api/auth/status endpoint
/// </summary>
public record AuthStatusResponse(bool Configured);

/// <summary>
/// Request for POST /api/auth/setup endpoint
/// </summary>
public record SetupRequest(string Password);

/// <summary>
/// Request for POST /api/auth/login endpoint
/// </summary>
public record LoginRequest(string Password);

/// <summary>
/// Response for POST /api/auth/login endpoint
/// </summary>
public record LoginResponse(string Token, DateTimeOffset ExpiresAt);

/// <summary>
/// Stored authentication data in auth.json
/// </summary>
public record AuthData(string PasswordHash);
