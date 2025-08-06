using Itenium.Forge.Core;

namespace Itenium.Forge.Settings;

/// <summary>
/// Implement this in your AppSettings
/// </summary>
public interface IForgeSettings
{
    public ForgeSettings Forge { get; }
}
