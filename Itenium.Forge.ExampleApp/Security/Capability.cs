namespace Itenium.Forge.ExampleApp.Security;

/// <summary>
/// Fine-grained capabilities that can be assigned to roles.
/// Configure role-capability mappings in appsettings.json under ForgeConfiguration:Security:RoleCapabilities.
/// </summary>
public enum Capability
{
    /// <summary>Read access to Resource X</summary>
    ReadResX,

    /// <summary>Write access to Resource X</summary>
    WriteResX,

    /// <summary>Read access to Resource Y</summary>
    ReadResY,

    /// <summary>Write access to Resource Y</summary>
    WriteResY
}
