using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Itenium.Forge.Logging;

/// <summary>
/// Ensures every request has a correlation ID.
/// Reads <c>x-correlation-id</c> from the incoming request header, or generates a new GUID when absent.
/// Echoes the value back on the response header so callers can trace their requests.
/// Pushes the value into Serilog's <see cref="LogContext"/> so it appears on every log entry for the request.
/// Also sets <see cref="HttpContext.TraceIdentifier"/> to keep the value consistent across the pipeline.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "x-correlation-id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        context.TraceIdentifier = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
