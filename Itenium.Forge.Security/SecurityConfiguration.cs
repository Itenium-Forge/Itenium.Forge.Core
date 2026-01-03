namespace Itenium.Forge.Security;

/// <summary>
/// Configuration for Keycloak/OIDC authentication.
/// Loaded from appsettings.json under ForgeConfiguration:Security
/// </summary>
internal class SecurityConfiguration
{
    /// <summary>
    /// The Keycloak realm URL (e.g., http://localhost:8080/realms/itenium)
    /// </summary>
    public string Authority { get; set; } = "";

    /// <summary>
    /// The expected audience claim in the JWT (typically the client ID)
    /// </summary>
    public string Audience { get; set; } = "";

    /// <summary>
    /// Whether to require HTTPS for metadata retrieval. Set to false for local development.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}
