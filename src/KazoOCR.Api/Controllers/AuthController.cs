using KazoOCR.Api.Models;
using KazoOCR.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace KazoOCR.Api.Controllers;

/// <summary>
/// Authentication endpoints for password-based login
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string SessionTokenHeader = "X-Session-Token";
    private const string SessionTokenCookie = "kazo_session";
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Get authentication status
    /// </summary>
    /// <returns>Whether a password has been configured</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(AuthStatusResponse), StatusCodes.Status200OK)]
    public ActionResult<AuthStatusResponse> GetStatus()
    {
        return Ok(new AuthStatusResponse(_authService.IsConfigured));
    }

    /// <summary>
    /// Set up the initial password (only available if no password is configured)
    /// </summary>
    /// <param name="request">Password setup request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or conflict</returns>
    [HttpPost("setup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Setup([FromBody] SetupRequest? request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Password is required" });
        }

        if (request.Password.Length < 8)
        {
            return BadRequest(new { error = "Password must be at least 8 characters" });
        }

        var success = await _authService.SetupPasswordAsync(request.Password, cancellationToken);

        if (!success)
        {
            _logger.LogWarning("Setup failed - password already configured");
            return Conflict(new { error = "Password already configured" });
        }

        _logger.LogInformation("Password setup completed successfully");
        return Ok(new { message = "Password configured successfully" });
    }

    /// <summary>
    /// Authenticate with password and receive a session token
    /// </summary>
    /// <param name="request">Login request with password</param>
    /// <returns>Session token on success</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest? request)
    {
        if (!_authService.IsConfigured)
        {
            return BadRequest(new { error = "Authentication not configured. Run setup first." });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Password is required" });
        }

        if (!_authService.ValidatePassword(request.Password))
        {
            _logger.LogWarning("Failed login attempt");
            return Unauthorized(new { error = "Invalid password" });
        }

        var loginResponse = _authService.CreateToken();

        // Set session cookie as well for browser-based access
        Response.Cookies.Append(SessionTokenCookie, loginResponse.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = loginResponse.ExpiresAt
        });

        _logger.LogInformation("User logged in successfully");
        return Ok(loginResponse);
    }

    /// <summary>
    /// Logout and invalidate the current session
    /// </summary>
    /// <returns>Success confirmation</returns>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult Logout()
    {
        // Get token from header or cookie
        var token = Request.Headers[SessionTokenHeader].FirstOrDefault()
                   ?? Request.Cookies[SessionTokenCookie];

        if (!string.IsNullOrEmpty(token))
        {
            _authService.InvalidateToken(token);
        }

        // Remove session cookie
        Response.Cookies.Delete(SessionTokenCookie);

        _logger.LogInformation("User logged out");
        return Ok(new { message = "Logged out successfully" });
    }
}
