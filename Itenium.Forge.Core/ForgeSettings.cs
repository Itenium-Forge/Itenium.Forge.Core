namespace Itenium.Forge.Core;

public class ForgeSettings
{
    public string ServiceName { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string Tenant { get; set; } = "";
    public string Environment { get; set; } = "";
    public string Application { get; set; } = "";

    public override string ToString() => $"{Application} :: {ServiceName} ({Environment}, {Tenant})";
}
