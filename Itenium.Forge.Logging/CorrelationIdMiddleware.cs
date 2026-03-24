using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Diagnostics;

namespace Itenium.Forge.Logging;

/// <summary>
/// Ensures a W3C trace ID is available for every request and propagates it throughout the pipeline.
///
/// When <c>Itenium.Forge.Telemetry</c> is installed, the OTel ASP.NET Core instrumentation has
/// already started an <see cref="Activity"/> (reading the incoming <c>traceparent</c> header) before
/// this middleware runs — so <see cref="Activity.Current"/> is already populated.
///
/// When <c>Itenium.Forge.Telemetry</c> is <b>not</b> installed, this middleware starts its own
/// fallback <see cref="Activity"/> and reads the incoming <c>traceparent</c> header itself, preserving
/// the caller's trace ID when present or generating a fresh one when absent.
///
/// In both cases the trace ID is written to:
/// <list type="bullet">
///   <item><see cref="HttpContext.TraceIdentifier"/> — picked up by ProblemDetails <c>Extensions["traceId"]</c></item>
///   <item>Serilog <see cref="LogContext"/> — every log entry for the request carries <c>TraceId</c></item>
/// </list>
/// </summary>
public class CorrelationIdMiddleware
{
    /// <summary>W3C TraceContext header name used for distributed trace propagation.</summary>
    public const string HeaderName = "traceparent";

    private const string FallbackActivityName = "ForgeRequest";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // OTel (Itenium.Forge.Telemetry) starts an Activity per request before this middleware runs.
        // When OTel is absent we start our own so trace IDs are still available.
        Activity? ownedActivity = Activity.Current is null
            ? StartFallbackActivity(context)
            : null;

        try
        {
            var traceId = Activity.Current?.TraceId.ToString()
                          ?? Guid.NewGuid().ToString("N");

            context.TraceIdentifier = traceId;

            using (LogContext.PushProperty("TraceId", traceId))
            {
                await _next(context);
            }
        }
        finally
        {
            ownedActivity?.Stop();
            ownedActivity?.Dispose();
        }
    }

    /// <summary>
    /// Creates and starts a W3C-format <see cref="Activity"/> for the current request.
    /// Reads the incoming <c>traceparent</c> header so an existing trace is continued rather
    /// than a new one being generated.
    /// </summary>
    private static Activity? StartFallbackActivity(HttpContext context)
    {
        var activity = new Activity(FallbackActivityName);

        if (context.Request.Headers.TryGetValue(HeaderName, out var traceparent)
            && TryParseTraceparent(traceparent.ToString(), out var traceId, out var spanId, out var flags))
        {
            activity.SetParentId(traceId, spanId, flags);
        }

        return activity.Start();
    }

    private static bool TryParseTraceparent(
        string traceparent,
        out ActivityTraceId traceId,
        out ActivitySpanId spanId,
        out ActivityTraceFlags flags)
    {
        traceId = default;
        spanId = default;
        flags = ActivityTraceFlags.None;

        // Expected format: 00-{32hex}-{16hex}-{2hex}
        var parts = traceparent.Split('-');
        if (parts.Length < 4 || parts[0] != "00") return false;

        try
        {
            traceId = ActivityTraceId.CreateFromString(parts[1].AsSpan());
            spanId = ActivitySpanId.CreateFromString(parts[2].AsSpan());
            flags = parts[3] == "01" ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
