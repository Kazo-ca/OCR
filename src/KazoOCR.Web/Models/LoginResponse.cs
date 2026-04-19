namespace KazoOCR.Web.Models;

/// <summary>
/// Login response from the API.
/// </summary>
public sealed class LoginResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether login was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the authentication token (if successful).
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the error message (if failed).
    /// </summary>
    public string? Error { get; set; }
}
