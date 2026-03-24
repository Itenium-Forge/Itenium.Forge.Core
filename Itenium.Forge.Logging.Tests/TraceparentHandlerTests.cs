using System.Diagnostics;
using System.Net;

namespace Itenium.Forge.Logging.Tests;

[TestFixture]
public class TraceparentHandlerTests
{
    [Test]
    public async Task SendAsync_WithActiveActivity_InjectsTraceparentHeader()
    {
        var activity = new Activity("test").Start();
        try
        {
            string? captured = null;
            using var invoker = MakeInvoker(req =>
            {
                req.Headers.TryGetValues("traceparent", out var values);
                captured = values?.FirstOrDefault();
            });

            await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://example.com"), CancellationToken.None);

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured, Does.StartWith("00-"));
            Assert.That(captured, Does.Contain(activity.TraceId.ToString()));
        }
        finally
        {
            activity.Stop();
        }
    }

    [Test]
    public async Task SendAsync_WithoutActivity_DoesNotInjectTraceparentHeader()
    {
        Assert.That(Activity.Current, Is.Null, "Precondition: no active activity");

        bool headerPresent = false;
        using var invoker = MakeInvoker(req =>
            headerPresent = req.Headers.Contains("traceparent"));

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://example.com"), CancellationToken.None);

        Assert.That(headerPresent, Is.False);
    }

    [Test]
    public async Task SendAsync_WithExistingTraceparentHeader_DoesNotOverwrite()
    {
        var activity = new Activity("test").Start();
        try
        {
            const string existing = "00-aaaabbbbccccddddaaaabbbbccccdddd-0000000000000001-01";
            string? captured = null;
            using var invoker = MakeInvoker(req =>
            {
                req.Headers.TryGetValues("traceparent", out var values);
                captured = values?.FirstOrDefault();
            });

            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
            request.Headers.Add("traceparent", existing);

            await invoker.SendAsync(request, CancellationToken.None);

            Assert.That(captured, Is.EqualTo(existing));
        }
        finally
        {
            activity.Stop();
        }
    }

    [Test]
    public async Task SendAsync_TraceparentFormat_IsValidW3C()
    {
        var activity = new Activity("test").Start();
        try
        {
            string? captured = null;
            using var invoker = MakeInvoker(req =>
            {
                req.Headers.TryGetValues("traceparent", out var values);
                captured = values?.FirstOrDefault();
            });

            await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://example.com"), CancellationToken.None);

            // Format: 00-{32hex}-{16hex}-{2hex}
            Assert.That(captured, Does.Match(@"^00-[0-9a-f]{32}-[0-9a-f]{16}-0[01]$"));
        }
        finally
        {
            activity.Stop();
        }
    }

    private static HttpMessageInvoker MakeInvoker(Action<HttpRequestMessage> onRequest)
    {
        var handler = new TraceparentHandler
        {
            InnerHandler = new LambdaHandler(req =>
            {
                onRequest(req);
                return new HttpResponseMessage(HttpStatusCode.OK);
            })
        };
        return new HttpMessageInvoker(handler);
    }

    private sealed class LambdaHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }
}
