using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Itenium.Forge.Logging.Tests;

[TestFixture]
public class RequestLoggingMiddlewareTests
{
    // ---------- skip conditions ----------

    [Test]
    public async Task Invoke_NonApiPath_SkipsLoggingAndCallsNext()
    {
        var nextCalled = false;
        var middleware = new RequestLoggingMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            NullLogger<RequestLoggingMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Path = "/health/live";
        context.Request.Method = HttpMethods.Get;

        await middleware.Invoke(context);

        Assert.That(nextCalled, Is.True);
    }

    [Test]
    public async Task Invoke_OptionsMethod_SkipsLoggingAndCallsNext()
    {
        var nextCalled = false;
        var middleware = new RequestLoggingMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            NullLogger<RequestLoggingMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/resource";
        context.Request.Method = HttpMethods.Options;

        await middleware.Invoke(context);

        Assert.That(nextCalled, Is.True);
    }

    // ---------- api path logging ----------

    [Test]
    public async Task Invoke_ApiGetWithNoQuery_CallsNext()
    {
        var nextCalled = false;
        var middleware = new RequestLoggingMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            NullLogger<RequestLoggingMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/items";
        context.Request.Method = HttpMethods.Get;

        await middleware.Invoke(context);

        Assert.That(nextCalled, Is.True);
    }

    [Test]
    public async Task Invoke_ApiGetWithQueryString_CallsNext()
    {
        var nextCalled = false;
        var middleware = new RequestLoggingMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            NullLogger<RequestLoggingMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/items";
        context.Request.Method = HttpMethods.Get;
        context.Request.QueryString = new QueryString("?page=1&size=20");

        await middleware.Invoke(context);

        Assert.That(nextCalled, Is.True);
    }

    [Test]
    public async Task Invoke_ApiPostWithBody_CallsNext()
    {
        var nextCalled = false;
        var middleware = new RequestLoggingMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            NullLogger<RequestLoggingMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/items";
        context.Request.Method = HttpMethods.Post;
        context.Request.Body = new MemoryStream("{\"name\":\"test\"}"u8.ToArray());
        context.Request.ContentLength = context.Request.Body.Length;

        await middleware.Invoke(context);

        Assert.That(nextCalled, Is.True);
    }

    [Test]
    public async Task Invoke_ApiPostWithBodyAndQuery_CallsNext()
    {
        var nextCalled = false;
        var middleware = new RequestLoggingMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            NullLogger<RequestLoggingMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/items";
        context.Request.Method = HttpMethods.Post;
        context.Request.QueryString = new QueryString("?draft=true");
        context.Request.Body = new MemoryStream("{\"name\":\"test\"}"u8.ToArray());
        context.Request.ContentLength = context.Request.Body.Length;

        await middleware.Invoke(context);

        Assert.That(nextCalled, Is.True);
    }

    [Test]
    public async Task Invoke_ApiDelete_CallsNext()
    {
        var nextCalled = false;
        var middleware = new RequestLoggingMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            NullLogger<RequestLoggingMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/items/1";
        context.Request.Method = HttpMethods.Delete;

        await middleware.Invoke(context);

        Assert.That(nextCalled, Is.True);
    }
}
