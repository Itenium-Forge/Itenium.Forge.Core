namespace Itenium.Forge.Core;

public class ForgeSettings
{
    /// <summary>
    /// The name of the service. Typically, this is in the form 'ApplicationName-ServiceName' (ex: app-backend)
    /// </summary>
    public string ServiceName { get; set; } = "";
    /// <summary>
    /// The team responsible for this service (optional)
    /// </summary>
    public string TeamName { get; set; } = "";
    /// <summary>
    /// The tenant/organization name (optional)
    /// </summary>
    public string Tenant { get; set; } = "";
    /// <summary>
    /// Deployment environment (e.g., Development, Staging, Production).
    /// When empty defaults to env.DOTNET_ENVIRONMENT, and then to 'Development'.
    /// </summary>
    public string Environment { get; set; } = "";
    /// <summary>
    /// The system name, which typically consists of a few services (ex: app)
    /// </summary>
    public string Application { get; set; } = "";

    public override string ToString() => $"{Application} :: {ServiceName} ({Environment}, {Tenant})";
}
