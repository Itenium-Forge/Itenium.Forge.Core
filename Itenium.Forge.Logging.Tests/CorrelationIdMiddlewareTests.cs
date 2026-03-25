using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

namespace Itenium.Forge.Logging.Tests;

[TestFixture]
public class CorrelationIdMiddlewareTests
{
    // ---------- TraceIdentifier ----------

    [Test]
    public async Task Invoke_WithActiveActivity_SetsTraceIdentifierFromActivity()
    {
        var activity = new Activity("test").Start();
        try
        {
            var context = new DefaultHttpContext();
            var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

            await middleware.Invoke(context);

            Assert.That(context.TraceIdentifier, Is.EqualTo(activity.TraceId.ToString()));
        }
        finally
        {
            activity.Stop();
        }
    }

    [Test]
    public async Task Invoke_WithoutActivity_GeneratesFreshW3CTraceId()
    {
        Assert.That(Activity.Current, Is.Null, "Precondition: no active activity");

        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.Invoke(context);

        // W3C trace IDs are 32 lowercase hex characters, not a dashed GUID
        Assert.That(context.TraceIdentifier, Has.Length.EqualTo(32));
        Assert.That(context.TraceIdentifier, Does.Match("^[0-9a-f]{32}$"));
    }

    [Test]
    public async Task Invoke_WithoutActivity_WithTraceparentHeader_ContinuesExistingTrace()
    {
        Assert.That(Activity.Current, Is.Null, "Precondition: no active activity");

        const string incomingTraceId = "4bf92f3577b34da6a3ce929d0e0e4736";
        var context = new DefaultHttpContext();
        context.Request.Headers["traceparent"] = $"00-{incomingTraceId}-00f067aa0ba902b7-01";
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.Invoke(context);

        Assert.That(context.TraceIdentifier, Is.EqualTo(incomingTraceId));
    }

    [Test]
    public async Task Invoke_FallbackActivity_CleanedUpAfterRequest()
    {
        Assert.That(Activity.Current, Is.Null, "Precondition: no active activity");

        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.Invoke(context);

        Assert.That(Activity.Current, Is.Null, "Fallback activity must not leak beyond the request");
    }

    // ---------- Serilog LogContext ----------

    [Test]
    public async Task Invoke_PushesTraceIdToLogContext_DuringNextExecution()
    {
        var activity = new Activity("test").Start();
        try
        {
            var sink = new CaptureSink();
            var logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Sink(sink)
                .CreateLogger();

            var context = new DefaultHttpContext();
            var middleware = new CorrelationIdMiddleware(_ =>
            {
                logger.Information("test event");
                return Task.CompletedTask;
            });

            await middleware.Invoke(context);

            var logEvent = sink.Events.Single();
            Assert.That(logEvent.Properties.ContainsKey("TraceId"), Is.True);
            var value = logEvent.Properties["TraceId"] as ScalarValue;
            Assert.That(value?.Value?.ToString(), Is.EqualTo(activity.TraceId.ToString()));
        }
        finally
        {
            activity.Stop();
        }
    }

    [Test]
    public async Task Invoke_TraceIdNotInLogContext_AfterRequestCompletes()
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
        Assert.That(logEvent.Properties.ContainsKey("TraceId"), Is.False);
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

    [Test]
    public async Task Invoke_WithMalformedTraceparentHex_IgnoresHeaderAndGeneratesFreshTrace()
    {
        // Header passes structural check (4 parts, version "00") but contains invalid hex — exercises catch block
        const string malformed = "00-ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ-ZZZZZZZZZZZZZZZZ-01";

        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = malformed;
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.Invoke(context);

        // Should not throw; a new trace ID is generated instead
        Assert.That(context.TraceIdentifier, Is.Not.Null.And.Not.Empty);
        Assert.That(context.TraceIdentifier, Is.Not.EqualTo(malformed));
    }

    // ---------- helpers ----------

    private class CaptureSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
