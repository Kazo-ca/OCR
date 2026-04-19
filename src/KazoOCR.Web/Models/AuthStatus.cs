namespace KazoOCR.Web.Models;

/// <summary>
/// Authentication status returned by the API.
/// </summary>
public sealed class AuthStatus
{
    /// <summary>
    /// Gets or sets a value indicating whether authentication is configured.
    /// </summary>
    public bool Configured { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is authenticated.
    /// </summary>
    public bool Authenticated { get; set; }
}
