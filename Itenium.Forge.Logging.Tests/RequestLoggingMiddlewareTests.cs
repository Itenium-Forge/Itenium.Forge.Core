using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Itenium.Forge.Logging.Tests;

[TestFixture]
public class RequestLoggingMiddlewareTests
{
    // ---------- skip conditions ----------
    // Non-API paths and OPTIONS requests skip logging but still call next.

    [Test]
    public async Task Invoke_NonApiPath_SkipsLogging()
    {
        var logger = new FakeLogger();
        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask, logger);

        var context = new DefaultHttpContext();
        context.Request.Path = "/health/live";
        context.Request.Method = HttpMethods.Get;

        await middleware.Invoke(context);

        Assert.That(logger.Messages, Is.Empty);
    }

    [Test]
    public async Task Invoke_OptionsMethod_SkipsLogging()
    {
        var logger = new FakeLogger();
        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask, logger);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/resource";
        context.Request.Method = HttpMethods.Options;

        await middleware.Invoke(context);

        Assert.That(logger.Messages, Is.Empty);
    }

    // ---------- api path logging ----------
    // API requests produce exactly two log entries: one before and one after the call to next.

    [Test]
    public async Task Invoke_ApiGetWithNoQuery_LogsRequestAndResponse()
    {
        var logger = new FakeLogger();
        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask, logger);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/items";
        context.Request.Method = HttpMethods.Get;

        await middleware.Invoke(context);

        Assert.That(logger.Messages, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Invoke_ApiGetWithQueryString_LogsQueryInRequest()
    {
        var logger = new FakeLogger();
        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask, logger);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/items";
        context.Request.Method = HttpMethods.Get;
        context.Request.QueryString = new QueryString("?page=1&size=20");

        await middleware.Invoke(context);

        Assert.That(logger.Messages[0], Does.Contain("page"));
    }

    [Test]
    public async Task Invoke_ApiPostWithBody_LogsBodyInRequest()
    {
        var logger = new FakeLogger();
        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask, logger);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/items";
        context.Request.Method = HttpMethods.Post;
        context.Request.Body = new MemoryStream("{\"name\":\"test\"}"u8.ToArray());
        context.Request.ContentLength = context.Request.Body.Length;

        await middleware.Invoke(context);

        Assert.That(logger.Messages[0], Does.Contain("test"));
    }

    [Test]
    public async Task Invoke_ApiPostWithBodyAndQuery_LogsBothInRequest()
    {
        var logger = new FakeLogger();
        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask, logger);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/items";
        context.Request.Method = HttpMethods.Post;
        context.Request.QueryString = new QueryString("?draft=true");
        context.Request.Body = new MemoryStream("{\"name\":\"test\"}"u8.ToArray());
        context.Request.ContentLength = context.Request.Body.Length;

        await middleware.Invoke(context);

        Assert.That(logger.Messages[0], Does.Contain("draft").And.Contain("test"));
    }

    [Test]
    public async Task Invoke_ApiDelete_LogsRequestAndResponse()
    {
        var logger = new FakeLogger();
        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask, logger);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/items/1";
        context.Request.Method = HttpMethods.Delete;

        await middleware.Invoke(context);

        Assert.That(logger.Messages, Has.Count.EqualTo(2));
    }

    // ---------- helpers ----------

    private sealed class FakeLogger : ILogger<RequestLoggingMiddleware>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }
}
