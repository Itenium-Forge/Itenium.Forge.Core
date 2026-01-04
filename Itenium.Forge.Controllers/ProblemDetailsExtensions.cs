using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Itenium.Forge.Controllers;

/// <summary>
/// Extensions for configuring RFC 7807 ProblemDetails responses for all HTTP errors.
/// </summary>
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// Adds ProblemDetails services with customization for consistent error responses.
    /// </summary>
    public static void AddForgeProblemDetails(this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance = context.HttpContext.Request.Path;

                var traceId = context.HttpContext.TraceIdentifier;
                if (!string.IsNullOrEmpty(traceId))
                {
                    context.ProblemDetails.Extensions["traceId"] = traceId;
                }
            };
        });
    }

    /// <summary>
    /// Configures the application to use ProblemDetails for exceptions and status code responses.
    /// Call this early in the middleware pipeline.
    /// </summary>
    public static void UseForgeProblemDetails(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
    }
}
