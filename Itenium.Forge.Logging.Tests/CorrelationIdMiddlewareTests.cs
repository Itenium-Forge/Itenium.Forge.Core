using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Itenium.Forge.Logging.Tests;

[TestFixture]
public class CorrelationIdMiddlewareTests
{
    // ---------- TraceIdentifier ----------

    [Test]
    public async Task Invoke_WithoutHeader_SetsTraceIdentifierToGeneratedGuid()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.Invoke(context);

        Assert.That(Guid.TryParse(context.TraceIdentifier, out _), Is.True);
    }

    [Test]
    public async Task Invoke_WithHeader_SetsTraceIdentifierToProvidedValue()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "my-correlation-id";
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.Invoke(context);

        Assert.That(context.TraceIdentifier, Is.EqualTo("my-correlation-id"));
    }

    // ---------- Serilog LogContext ----------

    [Test]
    public async Task Invoke_PushesCorrelationIdToLogContext_DuringNextExecution()
    {
        var sink = new CaptureSink();
        var logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(sink)
            .CreateLogger();

        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "trace-abc";
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            logger.Information("test event");
            return Task.CompletedTask;
        });

        await middleware.Invoke(context);

        var logEvent = sink.Events.Single();
        Assert.That(logEvent.Properties.ContainsKey("CorrelationId"), Is.True);
        var value = logEvent.Properties["CorrelationId"] as ScalarValue;
        Assert.That(value?.Value?.ToString(), Is.EqualTo("trace-abc"));
    }

    [Test]
    public async Task Invoke_CorrelationIdNotInLogContext_AfterRequestCompletes()
    {
        var sink = new CaptureSink();
        var logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(sink)
            .CreateLogger();

        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        await middleware.Invoke(context);

        // Log AFTER the middleware has finished — the using block has disposed the push
        logger.Information("after request");

        var logEvent = sink.Events.Single();
        Assert.That(logEvent.Properties.ContainsKey("CorrelationId"), Is.False);
    }

    // ---------- Pipeline ----------

    [Test]
    public async Task Invoke_AlwaysCallsNextMiddleware()
    {
        var context = new DefaultHttpContext();
        var nextCalled = false;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.Invoke(context);

        Assert.That(nextCalled, Is.True);
    }

    // ---------- helpers ----------

    private class CaptureSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
