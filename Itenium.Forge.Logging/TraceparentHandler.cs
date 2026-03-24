using System.Diagnostics;

namespace Itenium.Forge.Logging;

/// <summary>
/// Propagates the current W3C <c>traceparent</c> header to all outgoing <see cref="HttpClient"/> calls.
///
/// When <c>Itenium.Forge.Telemetry</c> is installed, the OTel SDK handles this automatically and will
/// overwrite the header with a correctly-scoped child span. This handler's value is then discarded,
/// so there is no conflict.
///
/// When <c>Itenium.Forge.Telemetry</c> is <b>not</b> installed, this handler ensures the current trace
/// context (started by <see cref="CorrelationIdMiddleware"/>) is forwarded to downstream services.
/// </summary>
internal sealed class TraceparentHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var activity = Activity.Current;
        if (activity != null && !request.Headers.Contains(CorrelationIdMiddleware.HeaderName))
        {
            var flags = activity.ActivityTraceFlags.HasFlag(ActivityTraceFlags.Recorded) ? "01" : "00";
            request.Headers.TryAddWithoutValidation(
                CorrelationIdMiddleware.HeaderName,
                $"00-{activity.TraceId}-{activity.SpanId}-{flags}");
        }

        return base.SendAsync(request, cancellationToken);
    }
}
