using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Itenium.Forge.Logging;

/// <summary>
/// We're not using the <see cref="Serilog.AspNetCore.RequestLoggingMiddleware"/>.
/// This one logs everything as Information including QueryString and Body.
/// Logs before and after the request.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Path.ToString().StartsWith("/api") || context.Request.Method == HttpMethods.Options)
        {
            await _next(context);
            return;
        }

        var timer = Stopwatch.StartNew();
        context.Request.EnableBuffering();
        var request = context.Request;

        string body = "";
        if (request.Method != HttpMethods.Get && request.Method != HttpMethods.Delete && request.ContentLength > 0)
        {
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }

        var qs = request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
        // TODO: check serialization of QueryString
        // var queryParams = JsonSerializer.Serialize(qs);

        if (qs.Count > 0 && body.Length > 0)
        {
            _logger.LogInformation("{Method} {Path} - Query: {@Query}, Body: {Body}", request.Method, request.Path, qs, body);
        }
        else if (qs.Count > 0)
        {
            _logger.LogInformation("{Method} {Path} - Query: {@Query}", request.Method, request.Path, qs);
        }
        else if (body.Length > 0)
        {
            _logger.LogInformation("{Method} {Path} - Body: {Body}", request.Method, request.Path, body);
        }
        else
        {
            _logger.LogInformation("{Method} {Path}", request.Method, request.Path);
        }


        await _next(context);


        _logger.LogInformation("{Method} {Path} - {StatusCode} in {Elapsed}", request.Method, request.Path, context.Response.StatusCode, timer.Elapsed);
    }
}
