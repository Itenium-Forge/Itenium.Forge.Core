using System.Reflection;
using Itenium.Forge.Core;
using Itenium.Forge.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;

namespace Itenium.Forge.Logging.Tests;

public class LoggingExtensionsTests
{
    [Test]
    public void AddForgeLogging_RegistersGlobalExceptionHandler()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddForgeSettings<AppSettings>();
        builder.AddForgeLogging();

        var exceptionHandler = builder.Services
            .Where(sd => sd.ServiceType == typeof(IExceptionHandler))
            .SingleOrDefault(sd => sd.ImplementationType == typeof(GlobalExceptionHandler));

        Assert.That(exceptionHandler, Is.Not.Null);
    }

    [Test]
    public void AppSettingsWithLokiUrl_AddsLokiSink()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddForgeSettings<AppSettings>("WithLoki");
        builder.AddForgeLogging();

        var app = builder.Build();
        ILogEventSink[] sinks = GetSinks(app);

        Assert.That(sinks, Has.Length.EqualTo(3));

        string[] sinkTypes = [..sinks.Select(x => x.GetType().FullName ?? "")];
        string[] expected = [
            "Serilog.Sinks.SystemConsole.ConsoleSink",
            "Serilog.Sinks.File.RollingFileSink",
            "Serilog.Sinks.Grafana.Loki.LokiSink"
        ];

        Assert.That(sinkTypes, Is.EqualTo(expected));
    }

    [Test]
    public void AppSettings_EmptyLokiUrl_DisablesLokiSink()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddForgeSettings<AppSettings>("NoLoki");
        builder.AddForgeLogging();

        var app = builder.Build();
        ILogEventSink[] sinks = GetSinks(app);

        Assert.That(sinks, Has.Length.EqualTo(2));

        bool hasLoki = sinks
            .Select(x => x.GetType().Name)
            .Any(x => x == "LokiSink");

        Assert.That(hasLoki, Is.False);
    }

    [Test]
    public void AppSettings_WithoutLokiUrl_DisablesLokiSink()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddForgeSettings<AppSettings>("NoLoki2");
        builder.AddForgeLogging();

        var app = builder.Build();
        ILogEventSink[] sinks = GetSinks(app);

        bool hasLoki = sinks
            .Select(x => x.GetType().Name)
            .Any(x => x == "LokiSink");

        Assert.That(hasLoki, Is.False);
    }

    [Test]
    public void AppSettings_CustomSerilogSection()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddForgeSettings<AppSettings>("CustomSerilog");
        builder.AddForgeLogging();

        var app = builder.Build();
        ILogEventSink[] sinks = GetSinks(app);

        Assert.That(sinks, Has.Length.EqualTo(1));
        string consoleSink = sinks.Single().GetType().Name;
        Assert.That(consoleSink, Is.EqualTo("ConsoleSink"));
    }

    [Test]
    public void AppSettings_CustomSerilogSection_IsNotPossibleWithLoggingPath()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddForgeSettings<AppSettings>("ConfigAndCustom");

        var ex = Assert.Throws<Exception>(() => builder.AddForgeLogging());
        Assert.That(ex.Message, Is.EqualTo("Cannot have a custom Serilog appSettings and set the ForgeConfiguration:Logging:FilePath"));
    }

    [Test]
    public void AppSettings_CustomSerilogSection_WithLoki()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddForgeSettings<AppSettings>("CustomSerilogWithLoki");
        builder.AddForgeLogging();

        var app = builder.Build();
        ILogEventSink[] sinks = GetSinks(app);

        string[] sinkTypes = [.. sinks.Select(x => x.GetType().Name)];
        string[] expected = [
            "ConsoleSink",
            "LokiSink"
        ];

        Assert.That(sinkTypes, Is.EqualTo(expected));
    }

    [Test]
    public void AppSettings_WithFilePathAndName_OverwritesDefaultLogLocation()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddForgeSettings<AppSettings>("WithFileName");
        builder.AddForgeLogging();

        var app = builder.Build();

        var filePath = app.Configuration["Serilog:WriteTo:1:Args:path"];
        Assert.That(filePath, Is.EqualTo(@"c:\temp\ExampleApp\le-logfile-.txt"));
    }

    [Test]
    public void AppSettings_WithFilePath_OverwritesDefaultLogLocation_AndAddsLogDashDotTxt()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddForgeSettings<AppSettings>();
        builder.AddForgeLogging();

        var app = builder.Build();

        var filePath = app.Configuration["Serilog:WriteTo:1:Args:path"];
        Assert.That(filePath, Is.EqualTo(@"c:\temp\ExampleApp\log-.txt"));
    }

    private static ILogEventSink[] GetSinks(WebApplication app)
    {
        var logger = app.Services.GetRequiredService<Serilog.ILogger>();

        FieldInfo? restrictedSinkFieldInfo = logger.GetType().GetField("_sink", BindingFlags.Instance | BindingFlags.NonPublic);
        ILogEventSink? restrictedSink = restrictedSinkFieldInfo?.GetValue(logger) as ILogEventSink;

        FieldInfo? aggregateSinkFieldInfo = restrictedSink?.GetType().GetField("_sink", BindingFlags.Instance | BindingFlags.NonPublic);
        ILogEventSink? aggregateSink = aggregateSinkFieldInfo?.GetValue(restrictedSink) as ILogEventSink;

        FieldInfo? sinksFieldInfo = aggregateSink?.GetType().GetField("_sinks", BindingFlags.Instance | BindingFlags.NonPublic);
        var sinks = sinksFieldInfo?.GetValue(aggregateSink) as ILogEventSink[];
        return sinks ?? [];
    }

    [Test]
    public void AddForgeLogging_AddsCustomEnrichers()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddForgeSettings<AppSettings>();
        builder.AddForgeLogging();

        var app = builder.Build();
        var enrichers = GetEnrichers(app);
        var enricherNames = enrichers
            .Select(x => x.GetType().Name)
            .ToArray();

        Assert.That(enricherNames, Has.Some.EqualTo("ClientIpEnricher"));
        Assert.That(enricherNames, Has.Some.EqualTo("MachineNameEnricher"));
        Assert.That(enricherNames, Has.Some.EqualTo("ThreadIdEnricher"));
        Assert.That(enricherNames, Has.Some.EqualTo("ClientHeaderEnricher")); // x-correlation-id header
    }

    [Test]
    public void AddForgeLogging_AddsContext()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddForgeSettings<AppSettings>();
        builder.AddForgeLogging();

        var app = builder.Build();
        var enrichers = GetProperties(GetEnrichers(app));

        Assert.That(enrichers, Contains.Item(new KeyValuePair<string, string>("service_name", "LoggingTests-Tests")));
        Assert.That(enrichers, Contains.Item(new KeyValuePair<string, string>("TeamName", "TeamName")));
        Assert.That(enrichers, Contains.Item(new KeyValuePair<string, string>("Tenant", "TenantName")));
        Assert.That(enrichers, Contains.Item(new KeyValuePair<string, string>("Environment", "Development")));
        Assert.That(enrichers, Contains.Item(new KeyValuePair<string, string>("Application", "LoggingTests")));
    }

    [Test]
    public void AddForgeLogging_TeamAndTenantContext_IsOptional()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddForgeSettings<AppSettings>("NoTeamNoTenant");
        builder.AddForgeLogging();

        var app = builder.Build();
        var enrichers = GetProperties(GetEnrichers(app));

        Assert.That(enrichers, Has.None.Property("Key").EqualTo("TeamName"));
        Assert.That(enrichers, Has.None.Property("Key").EqualTo("Tenant"));
    }

    private static ILogEventEnricher[] GetEnrichers(WebApplication app)
    {
        var logger = app.Services.GetRequiredService<Serilog.ILogger>();

        FieldInfo? restrictedSinkFieldInfo = logger.GetType().GetField("_sink", BindingFlags.Instance | BindingFlags.NonPublic);
        ILogEventSink? restrictedSink = restrictedSinkFieldInfo?.GetValue(logger) as ILogEventSink;

        FieldInfo? aggregateFieldInfo = restrictedSink?.GetType().GetField("_enricher", BindingFlags.Instance | BindingFlags.NonPublic);
        var aggregate = aggregateFieldInfo?.GetValue(restrictedSink) as ILogEventEnricher;

        FieldInfo? enrichersFieldInfo = aggregate?.GetType().GetField("_enrichers", BindingFlags.Instance | BindingFlags.NonPublic);
        if (enrichersFieldInfo?.GetValue(aggregate) is not ILogEventEnricher[] enrichers)
            return [];

        return enrichers;
    }

    private static KeyValuePair<string, string>[] GetProperties(ILogEventEnricher[] enrichers)
    {
        return enrichers
            .Select(enricher =>
            {
                var nameFieldInfo = enricher.GetType().GetField("_name", BindingFlags.Instance | BindingFlags.NonPublic);
                var valueFieldInfo = enricher.GetType().GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic);
                if (nameFieldInfo == null || valueFieldInfo == null)
                    return new KeyValuePair<string, string>("", "");

                var name = nameFieldInfo.GetValue(enricher) as string;
                var value = valueFieldInfo.GetValue(enricher) as string;
                return new KeyValuePair<string, string>(name ?? "", value ?? "");
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .ToArray();
    }

    private class AppSettings : IForgeSettings
    {
        public ForgeSettings Forge { get; } = new();
    }
}
