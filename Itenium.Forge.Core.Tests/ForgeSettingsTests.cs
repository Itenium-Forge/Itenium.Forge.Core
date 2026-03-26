using Itenium.Forge.Core;

namespace Itenium.Forge.Core.Tests;

[TestFixture]
public class ForgeSettingsTests
{
    [Test]
    public void ToString_FormatsApplicationServiceNameEnvironmentAndTenant()
    {
        var settings = new ForgeSettings
        {
            Application = "app",
            ServiceName = "app-backend",
            Environment = "Production",
            Tenant = "acme"
        };

        Assert.That(settings.ToString(), Is.EqualTo("app :: app-backend (Production, acme)"));
    }
}
