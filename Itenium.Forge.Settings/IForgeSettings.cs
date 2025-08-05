namespace Itenium.Forge.Settings;

/// <summary>
/// Implement this in your AppSettings
/// </summary>
public interface IForgeSettings
{
    public ForgeSettings ForgeSettings { get; }
}
