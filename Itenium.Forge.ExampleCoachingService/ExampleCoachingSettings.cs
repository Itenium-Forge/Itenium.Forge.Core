using Itenium.Forge.Core;
using Itenium.Forge.Settings;

namespace Itenium.Forge.ExampleCoachingService;

public class ExampleCoachingSettings : IForgeSettings
{
    public ForgeSettings Forge { get; } = new();
}
