using System.ComponentModel.DataAnnotations;
using Itenium.Forge.ExampleCoachingService.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Itenium.Forge.HttpClients.Tests;

[TestFixture]
public class HttpClientsOptionsTests
{
    [Test]
    public void Options_WhenBaseUrlValid_ResolvesWithDefaults()
    {
        var app = BuildApp("https://api.example.com");

        var entry = app.Services.GetRequiredService<IOptions<HttpClientsOptions>>().Value.HttpClients["MyService"];
        Assert.That(entry.BaseUrl, Is.EqualTo("https://api.example.com"));
        Assert.That(entry.TimeoutSeconds, Is.EqualTo(30));
        Assert.That(entry.HealthPath, Is.EqualTo("/health/ready"));
    }

    [Test]
    public void Options_WhenBaseUrlMalformed_ThrowsOnValidation()
    {
        var app = BuildApp("not-a-url");

        Assert.Throws<OptionsValidationException>(() =>
        {
            _ = app.Services.GetRequiredService<IOptions<HttpClientsOptions>>().Value;
        });
    }

    [Test]
    public void Entry_RequiredBaseUrl_FailsValidation()
    {
        var entry = new HttpClientEntryOptions { BaseUrl = "" };
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(entry, new ValidationContext(entry), results, validateAllProperties: true);

        Assert.That(results.Any(r => r.MemberNames.Contains("BaseUrl")), Is.True);
    }

    [Test]
    public void Entry_TimeoutSeconds_OutOfRange_FailsValidation()
    {
        var entry = new HttpClientEntryOptions { BaseUrl = "https://api.example.com", TimeoutSeconds = 0 };
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(entry, new ValidationContext(entry), results, validateAllProperties: true);

        Assert.That(results.Any(r => r.MemberNames.Contains("TimeoutSeconds")), Is.True);
    }

    [Test]
    public void Entry_HealthPath_DefaultsToHealthReady()
    {
        var entry = new HttpClientEntryOptions { BaseUrl = "https://api.example.com" };
        Assert.That(entry.ResolvedHealthCheckUrl, Is.EqualTo("https://api.example.com/health/ready"));
    }

    [Test]
    public void Entry_BaseUrl_TrailingSlash_IsStrippedFromHealthCheckUrl()
    {
        var entry = new HttpClientEntryOptions { BaseUrl = "https://api.example.com/" };
        Assert.That(entry.ResolvedHealthCheckUrl, Is.EqualTo("https://api.example.com/health/ready"));
    }

    [Test]
    public void Entry_HealthPath_CustomPath_IsUsed()
    {
        var entry = new HttpClientEntryOptions { BaseUrl = "https://api.example.com", HealthPath = "/healthz" };
        Assert.That(entry.ResolvedHealthCheckUrl, Is.EqualTo("https://api.example.com/healthz"));
    }

    [Test]
    public void AddForgeHttpClient_RegistersHealthCheck_TaggedReady()
    {
        var app = BuildApp("https://api.example.com");

        var registrations = app.Services
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        var registration = registrations.FirstOrDefault(r => r.Name == "http-MyService");
        Assert.That(registration, Is.Not.Null);
        Assert.That(registration!.Tags, Contains.Item("ready"));
    }

    [Test]
    public void AddForgeHttpClient_ConfigureHttpClient_SetsBaseAddressAndTimeout()
    {
        var app = BuildApp("https://api.example.com");

        var factory = app.Services.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("MyService");

        Assert.That(client.BaseAddress, Is.EqualTo(new Uri("https://api.example.com")));
        Assert.That(client.Timeout, Is.EqualTo(TimeSpan.FromSeconds(30)));
    }

    [Test]
    public void AddForgeHttpClient_HealthCheckFactory_CreatesCheck()
    {
        var app = BuildApp("https://api.example.com");

        var registration = app.Services
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value
            .Registrations.First(r => r.Name == "http-MyService");

        Assert.That(registration.Factory(app.Services), Is.InstanceOf<IHealthCheck>());
    }

    [Test]
    public void AddForgeHttpClient_EntryMissingFromConfig_ThrowsOnFactoryInvocation()
    {
        // Client registered but no matching config entry — ResolveEntry throws
        var builder = WebApplication.CreateBuilder();
        builder.AddForgeHttpClient<IFakeClient>("MyService");
        var app = builder.Build();

        var registration = app.Services
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value
            .Registrations.First(r => r.Name == "http-MyService");

        Assert.Throws<InvalidOperationException>(() => registration.Factory(app.Services));
    }

    [Test]
    public void AddForgeHttpClient_CalledMultipleTimes_RegistersOptionsOnce()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["ForgeConfiguration:HttpClients:ServiceA:BaseUrl"] = "https://a.example.com";
        builder.Configuration["ForgeConfiguration:HttpClients:ServiceB:BaseUrl"] = "https://b.example.com";
        builder.AddForgeHttpClient<IFakeClient>("ServiceA");
        builder.AddForgeHttpClient<IFakeClientB>("ServiceB");

        var options = builder.Build().Services
            .GetRequiredService<IOptions<HttpClientsOptions>>().Value;

        Assert.That(options.HttpClients.ContainsKey("ServiceA"), Is.True);
        Assert.That(options.HttpClients.ContainsKey("ServiceB"), Is.True);
    }

    private static WebApplication BuildApp(string baseUrl)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["ForgeConfiguration:HttpClients:MyService:BaseUrl"] = baseUrl;
        builder.AddForgeHttpClient<IFakeClient>("MyService");
        return builder.Build();
    }

    private interface IFakeClient { }
    private interface IFakeClientB { }
}
