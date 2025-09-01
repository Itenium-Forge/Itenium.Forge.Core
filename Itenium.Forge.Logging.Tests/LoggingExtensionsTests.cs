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

    private class AppSettings : IForgeSettings
    {
        public ForgeSettings Forge { get; } = new();
    }
}
