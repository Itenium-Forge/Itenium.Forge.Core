using Itenium.Forge.Core;

namespace Itenium.Forge.Core.Tests;

[TestFixture]
public class ForgeSettingsTests
{
    [Test]
    public void DefaultValues_AreEmptyStrings()
    {
        var settings = new ForgeSettings();

        Assert.That(settings.ServiceName, Is.Empty);
        Assert.That(settings.TeamName, Is.Empty);
        Assert.That(settings.Tenant, Is.Empty);
        Assert.That(settings.Environment, Is.Empty);
        Assert.That(settings.Application, Is.Empty);
    }

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
