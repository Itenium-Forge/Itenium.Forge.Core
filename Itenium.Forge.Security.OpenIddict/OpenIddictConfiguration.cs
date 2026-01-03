namespace Itenium.Forge.Security.OpenIddict;

/// <summary>
/// Configuration for OpenIddict identity server.
/// Loaded from appsettings.json under ForgeConfiguration:Security
/// </summary>
public class OpenIddictConfiguration
{
    /// <summary>
    /// The issuer URL for tokens (typically your application URL).
    /// If not set, defaults to the application's base URL.
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// Client ID for the default SPA client.
    /// </summary>
    public string ClientId { get; set; } = "forge-spa";

    /// <summary>
    /// Display name for the default client.
    /// </summary>
    public string ClientDisplayName { get; set; } = "Forge SPA";

    /// <summary>
    /// Allowed redirect URIs for the SPA client (comma-separated).
    /// </summary>
    public string RedirectUris { get; set; } = "http://localhost:3000/callback,http://localhost:5173/callback";

    /// <summary>
    /// Allowed post-logout redirect URIs (comma-separated).
    /// </summary>
    public string PostLogoutRedirectUris { get; set; } = "http://localhost:3000,http://localhost:5173";

    /// <summary>
    /// Access token lifetime in minutes.
    /// </summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token lifetime in days.
    /// </summary>
    public int RefreshTokenLifetimeDays { get; set; } = 14;
}
