namespace Itenium.Forge.Settings.Tests;

public class ForgeSettingsTests
{
    [Test]
    public void Load_SetsCustomProperties()
    {
        var settings = ForgeSettings.Load<AppSettings>("Development");
        Assert.That(settings.MyProp, Is.True);
    }

    [Test]
    public void Load_SetsForgeProperties()
    {
        var settings = ForgeSettings.Load<AppSettings>("Development");
        Assert.That(settings.Forge.Application, Is.EqualTo("TodoApp"));
        Assert.That(settings.Forge.TeamName, Is.EqualTo("Core"));
        Assert.That(settings.Forge.Tenant, Is.EqualTo("itenium"));
        Assert.That(settings.Forge.ServiceName, Is.EqualTo("TodoApp.WebApi"));
    }

    [Test]
    public void Load_AddsExtraSettings_WhenEnvironmentIsSet()
    {
        var settings = ForgeSettings.Load<AppSettings>("Production");
        Assert.That(settings.Forge.Application, Is.EqualTo("TodoApp"));
        Assert.That(settings.Forge.TeamName, Is.EqualTo("Core"));
        Assert.That(settings.Forge.Tenant, Is.EqualTo("itenium"));
        Assert.That(settings.Forge.ServiceName, Is.EqualTo("TodoApp.WebApi"));
        Assert.That(settings.Forge.Environment, Is.EqualTo("Production"));
    }

    [Test]
    public void Load_CrashesWhenEnvironments_DoNotMatch()
    {
        var ex = Assert.Throws<Exception>(() => ForgeSettings.Load<AppSettings>("Staging"));
        Assert.That(ex.Message, Does.Contain("Environments from"));
        Assert.That(ex.Message, Does.Contain("do not match"));
    }

    [Test]
    public void Load_AllowsEmptyEnvironment_AndDefaultsToEnvironmentVariable()
    {
        var settings = ForgeSettings.Load<AppSettings>("Test");
        Assert.That(settings.Forge.Environment, Is.EqualTo("Test"));
    }

    [Test]
    public void Load_AllowsOverride_OfCustomProperties()
    {
        var settings = ForgeSettings.Load<AppSettings>("Test");
        Assert.That(settings.MyProp, Is.False);
    }

    private class AppSettings : IForgeSettings
    {
        public ForgeSettings Forge { get; } = new();
        public bool MyProp { get; set; }
    }
}
