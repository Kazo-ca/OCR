using System.Text.Json.Serialization;

namespace KazoOCR.Web.Models;

/// <summary>
/// Authentication status returned by the API.
/// API returns only "configured" - "authenticated" is derived client-side from token presence.
/// </summary>
public sealed class AuthStatus
{
    /// <summary>
    /// Gets or sets a value indicating whether authentication is configured.
    /// This is the only field returned by the API.
    /// </summary>
    [JsonPropertyName("configured")]
    public bool Configured { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is authenticated.
    /// This is NOT returned by the API - it's set client-side based on token presence.
    /// </summary>
    [JsonIgnore]
    public bool Authenticated { get; set; }
}
