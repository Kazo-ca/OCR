namespace KazoOCR.Web.Models;

/// <summary>
/// First-run setup request for password creation.
/// </summary>
public sealed class SetupRequest
{
    /// <summary>
    /// Gets or sets the new password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password confirmation.
    /// </summary>
    public string ConfirmPassword { get; set; } = string.Empty;
}
