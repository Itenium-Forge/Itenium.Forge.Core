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
    public async Task Invoke_WithoutActivity_FallsBackToGeneratedGuid()
    {
        Assert.That(Activity.Current, Is.Null, "Precondition: no active activity");

        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.Invoke(context);

        Assert.That(Guid.TryParse(context.TraceIdentifier, out _), Is.True);
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

    // ---------- helpers ----------

    private class CaptureSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
