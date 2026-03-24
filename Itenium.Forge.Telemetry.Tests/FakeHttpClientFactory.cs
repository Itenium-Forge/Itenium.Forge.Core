using System.Net;

namespace Itenium.Forge.Telemetry.Tests;

/// <summary>
/// Builds an <see cref="IHttpClientFactory"/> whose clients never make real network calls.
/// </summary>
internal static class FakeHttpClientFactory
{
    public static IHttpClientFactory Returning(HttpStatusCode status)
        => FromHandler(new FakeHandler(_ => new HttpResponseMessage(status)));

    public static IHttpClientFactory Throwing(Exception exception)
        => FromHandler(new FakeHandler(_ => throw exception));

    private static IHttpClientFactory FromHandler(FakeHandler handler)
        => new SingleClientFactory(new HttpClient(handler));

    private class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }

    private class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
}
