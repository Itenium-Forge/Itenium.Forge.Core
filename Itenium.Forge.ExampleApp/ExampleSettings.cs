using Itenium.Forge.Core;
using Itenium.Forge.Settings;

namespace Itenium.Forge.ExampleApp;

public class ExampleSettings : IForgeSettings
{
    public ForgeSettings Forge { get; } = new();
}
