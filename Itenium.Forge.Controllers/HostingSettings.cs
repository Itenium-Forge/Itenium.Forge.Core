namespace Itenium.Forge.Controllers;

public class HostingSettings
{
    public string[] AllowedHosts { get; set; } = [];
    public string CorsOrigins { get; set; } = "";
}
