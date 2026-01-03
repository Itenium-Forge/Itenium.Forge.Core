using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Itenium.Forge.Security;

/// <summary>
/// Common security extension methods.
/// Use with Itenium.Forge.Security.Keycloak or Itenium.Forge.Security.OpenIddict.
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Registers common security services (ICurrentUser, IHttpContextAccessor).
    /// Called automatically by AddForgeKeycloak() or AddForgeOpenIddict().
    /// </summary>
    public static void AddForgeSecurityCore(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
    }

    /// <summary>
    /// Adds authentication, authorization, and user logging middleware to the pipeline.
    /// </summary>
    public static void UseForgeSecurity(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<UserLoggingMiddleware>();
    }
}
