namespace KazoOCR.Web.Models;

/// <summary>
/// Login request for authentication.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
