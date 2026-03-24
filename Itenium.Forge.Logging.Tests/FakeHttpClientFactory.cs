using System.Net;

namespace Itenium.Forge.Logging.Tests;

/// <summary>
/// Builds an <see cref="IHttpClientFactory"/> whose clients never make real network calls.
/// </summary>
internal static class FakeHttpClientFactory
{
    public static IHttpClientFactory Returning(HttpStatusCode status)
        => FromHandler(new FakeHandler(_ => new HttpResponseMessage(status)));

    public static IHttpClientFactory Throwing(Exception exception)
        => FromHandler(new FakeHandler(_ => throw exception));

    public static IHttpClientFactory Intercepting(HttpStatusCode status, Action<Uri?> onRequest)
        => FromHandler(new FakeHandler(request =>
        {
            onRequest(request.RequestUri);
            return new HttpResponseMessage(status);
        }));

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
