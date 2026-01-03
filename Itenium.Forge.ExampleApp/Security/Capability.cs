namespace Itenium.Forge.ExampleApp.Security;

/// <summary>
/// Fine-grained capabilities that can be assigned to roles.
/// Configure role-capability mappings in appsettings.json under ForgeConfiguration:Security:RoleCapabilities.
/// </summary>
public enum Capability
{
    ReadResX,
    WriteResX,
}
