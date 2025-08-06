using Itenium.Forge.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Itenium.Forge.Settings.Tests;

public class ForgeSettingsTests
{
    [Test]
    public void Load_SetsCustomProperties()
    {
        var builder = WebApplication.CreateBuilder();
        var settings = builder.LoadConfiguration<AppSettings>("Development");
        Assert.That(settings.MyProp, Is.True);
    }

    [Test]
    public void Load_SetsBuilderEnvironmentName()
    {
        var builder = WebApplication.CreateBuilder();
        var settings = builder.LoadConfiguration<AppSettings>("Test");
        Assert.That(builder.Environment.EnvironmentName, Is.EqualTo("Test"));
    }

    [Test]
    public void Load_SetsBuilderConfiguration()
    {
        var builder = WebApplication.CreateBuilder();
        builder.LoadConfiguration<AppSettings>("Test");
        var settings = builder.Configuration.GetSection("Forge").Get<ForgeSettings>();
        Assert.That(settings?.TeamName, Is.EqualTo("TestTeam"));
    }

    [Test]
    public void Load_SetsForgeProperties()
    {
        var builder = WebApplication.CreateBuilder();
        var settings = builder.LoadConfiguration<AppSettings>("Development");
        Assert.Multiple(() =>
        {
            Assert.That(settings.Forge.Application, Is.EqualTo("TodoApp"));
            Assert.That(settings.Forge.TeamName, Is.EqualTo("Core"));
            Assert.That(settings.Forge.Tenant, Is.EqualTo("itenium"));
            Assert.That(settings.Forge.ServiceName, Is.EqualTo("TodoApp.WebApi"));
        });
    }

    [Test]
    public void Load_AddsExtraSettings_WhenEnvironmentIsSet()
    {
        var builder = WebApplication.CreateBuilder();
        var settings = builder.LoadConfiguration<AppSettings>("Production");
        Assert.Multiple(() =>
        {
            Assert.That(settings.Forge.Application, Is.EqualTo("TodoApp"));
            Assert.That(settings.Forge.TeamName, Is.EqualTo("Core"));
            Assert.That(settings.Forge.Tenant, Is.EqualTo("itenium"));
            Assert.That(settings.Forge.ServiceName, Is.EqualTo("TodoApp.WebApi"));
            Assert.That(settings.Forge.Environment, Is.EqualTo("Production"));
        });
    }

    [Test]
    public void Load_CrashesWhenEnvironments_DoNotMatch()
    {
        var builder = WebApplication.CreateBuilder();
        var ex = Assert.Throws<Exception>(() => builder.LoadConfiguration<AppSettings>("Staging"));
        Assert.That(ex.Message, Does.Contain("Environments from"));
        Assert.That(ex.Message, Does.Contain("do not match"));
    }

    [Test]
    public void Load_AllowsEmptyEnvironment_AndDefaultsToEnvironmentVariable()
    {
        var builder = WebApplication.CreateBuilder();
        var settings = builder.LoadConfiguration<AppSettings>("Test");
        Assert.That(settings.Forge.Environment, Is.EqualTo("Test"));
    }

    [Test]
    public void Load_AllowsOverride_OfCustomProperties()
    {
        var builder = WebApplication.CreateBuilder();
        var settings = builder.LoadConfiguration<AppSettings>("Test");
        Assert.That(settings.MyProp, Is.False);
    }

    [Test]
    public void Load_InjectsSettingsAndForge()
    {
        var builder = WebApplication.CreateBuilder();
        builder.LoadConfiguration<AppSettings>("Development");
        var app = builder.Build();

        app.Services.GetRequiredService<AppSettings>();
        app.Services.GetRequiredService<ForgeSettings>();
    }

    private class AppSettings : IForgeSettings
    {
        public ForgeSettings Forge { get; } = new();
        public bool MyProp { get; set; }
    }
}
