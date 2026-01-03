using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Itenium.Forge.Security;

/// <summary>
/// Middleware that enriches the Serilog log context with user information.
/// </summary>
internal class UserLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public UserLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUser currentUser)
    {
        if (currentUser.IsAuthenticated)
        {
            using (LogContext.PushProperty("UserId", currentUser.UserId ?? "Unknown"))
            using (LogContext.PushProperty("UserName", currentUser.UserName ?? "Unknown"))
            {
                await _next(context);
            }
        }
        else
        {
            using (LogContext.PushProperty("UserId", "Anonymous"))
            using (LogContext.PushProperty("UserName", "Anonymous"))
            {
                await _next(context);
            }
        }
    }
}
