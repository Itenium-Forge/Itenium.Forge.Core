using Itenium.Forge.Core;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Itenium.Forge.Logging;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly ForgeSettings _settings;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, ForgeSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Exception occurred: {ErrorMessage}", exception.Message);

        ProblemDetails problemDetails;
        if (_settings.Environment == "Development")
        {
            problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server error",
                Detail = exception.ToString()
            };
        }
        else
        {
            problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server error",
                Detail = "CorrelationId: " // TODO: CorrelationId
            };
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
