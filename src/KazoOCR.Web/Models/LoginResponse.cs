using System.Text.Json.Serialization;

namespace KazoOCR.Web.Models;

/// <summary>
/// Login response from the API.
/// Matches KazoOCR.Api.Models.LoginResponse: record LoginResponse(string Token, DateTimeOffset ExpiresAt)
/// </summary>
public sealed class LoginResponse
{
    /// <summary>
    /// Gets or sets the authentication token.
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token expiration timestamp.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Gets a value indicating whether the login was successful (token is present).
    /// </summary>
    [JsonIgnore]
    public bool Success => !string.IsNullOrEmpty(Token);
}
