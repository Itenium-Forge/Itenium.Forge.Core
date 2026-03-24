using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Diagnostics;

namespace Itenium.Forge.Logging;

/// <summary>
/// Extracts the W3C trace ID from the active <see cref="Activity"/> (populated from the
/// incoming <c>traceparent</c> header by the OpenTelemetry ASP.NET Core instrumentation)
/// and makes it available throughout the request pipeline.
///
/// Sets <see cref="HttpContext.TraceIdentifier"/> so ProblemDetails picks it up automatically,
/// and pushes <c>TraceId</c> into Serilog's <see cref="LogContext"/> so every log entry
/// for the request carries the trace ID — correlating logs with traces in Grafana Tempo.
///
/// Falls back to a generated GUID when no active trace exists (e.g. in tests without OTel).
/// </summary>
public class CorrelationIdMiddleware
{
    /// <summary>W3C TraceContext header name used for distributed trace propagation.</summary>
    public const string HeaderName = "traceparent";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString()
                      ?? Guid.NewGuid().ToString();

        context.TraceIdentifier = traceId;

        using (LogContext.PushProperty("TraceId", traceId))
        {
            await _next(context);
        }
    }
}
