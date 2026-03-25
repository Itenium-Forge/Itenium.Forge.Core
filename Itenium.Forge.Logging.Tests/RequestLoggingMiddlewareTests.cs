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
        var (logger, middleware) = Build();

        var context = new DefaultHttpContext();
        context.Request.Path = "/health/live";
        context.Request.Method = HttpMethods.Get;

        await middleware.Invoke(context);

        Assert.That(logger.Messages, Is.Empty);
    }

    [Test]
    public async Task Invoke_OptionsMethod_SkipsLogging()
    {
        var (logger, middleware) = Build();

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
        var (logger, middleware) = Build();

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/items";
        context.Request.Method = HttpMethods.Get;

        await middleware.Invoke(context);

        Assert.That(logger.Messages, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Invoke_ApiGetWithQueryString_LogsQueryInRequest()
    {
        var (logger, middleware) = Build();

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
        var (logger, middleware) = Build();

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
        var (logger, middleware) = Build();

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
        var (logger, middleware) = Build();

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/items/1";
        context.Request.Method = HttpMethods.Delete;

        await middleware.Invoke(context);

        Assert.That(logger.Messages, Has.Count.EqualTo(2));
    }

    // ---------- field masking — body ----------

    [Test]
    public async Task Invoke_PostWithPasswordField_PasswordIsMaskedInLog()
    {
        var (logger, middleware) = Build();

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/login";
        context.Request.Method = HttpMethods.Post;
        context.Request.Body = new MemoryStream("{\"username\":\"alice\",\"password\":\"s3cret\"}"u8.ToArray());
        context.Request.ContentLength = context.Request.Body.Length;

        await middleware.Invoke(context);

        Assert.That(logger.Messages[0], Does.Contain("alice"));
        Assert.That(logger.Messages[0], Does.Contain("***"));
        Assert.That(logger.Messages[0], Does.Not.Contain("s3cret"));
    }

    [Test]
    public async Task Invoke_PostWithNonJsonBody_BodyLoggedAsIs()
    {
        var (logger, middleware) = Build();

        const string rawBody = "password=s3cret&username=alice";
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/login";
        context.Request.Method = HttpMethods.Post;
        context.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(rawBody));
        context.Request.ContentLength = context.Request.Body.Length;

        await middleware.Invoke(context);

        Assert.That(logger.Messages[0], Does.Contain("password=s3cret"));
    }

    // ---------- field masking — query string ----------

    [Test]
    public async Task Invoke_GetWithSensitiveQueryParam_ValueIsMasked()
    {
        var (logger, middleware) = Build();

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/items";
        context.Request.Method = HttpMethods.Get;
        context.Request.QueryString = new QueryString("?api_key=supersecret&page=1");

        await middleware.Invoke(context);

        Assert.That(logger.Messages[0], Does.Contain("***"));
        Assert.That(logger.Messages[0], Does.Not.Contain("supersecret"));
        Assert.That(logger.Messages[0], Does.Contain("page"));
    }

    // ---------- custom masking options ----------

    [Test]
    public async Task Invoke_CustomMaskedField_IsMasked()
    {
        var options = new FieldMaskingOptions();
        options.AddFields("credit_card");
        var (logger, middleware) = Build(options);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/payments";
        context.Request.Method = HttpMethods.Post;
        context.Request.Body = new MemoryStream("{\"credit_card\":\"4111111111111111\"}"u8.ToArray());
        context.Request.ContentLength = context.Request.Body.Length;

        await middleware.Invoke(context);

        Assert.That(logger.Messages[0], Does.Contain("***"));
        Assert.That(logger.Messages[0], Does.Not.Contain("4111111111111111"));
    }

    [Test]
    public async Task Invoke_SetFields_ReplacesDefaults()
    {
        var options = new FieldMaskingOptions();
        options.SetFields("credit_card");
        var (logger, middleware) = Build(options);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/payments";
        context.Request.Method = HttpMethods.Post;
        context.Request.Body = new MemoryStream("{\"credit_card\":\"4111\",\"password\":\"unchanged\"}"u8.ToArray());
        context.Request.ContentLength = context.Request.Body.Length;

        await middleware.Invoke(context);

        // credit_card is masked; password is NOT masked (defaults replaced)
        Assert.That(logger.Messages[0], Does.Contain("unchanged"));
        Assert.That(logger.Messages[0], Does.Not.Contain("4111"));
    }

    // ---------- helpers ----------

    private static (FakeLogger, RequestLoggingMiddleware) Build(FieldMaskingOptions? options = null)
    {
        var logger = new FakeLogger();
        var middleware = new RequestLoggingMiddleware(
            _ => Task.CompletedTask,
            logger,
            options ?? new FieldMaskingOptions());
        return (logger, middleware);
    }

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
