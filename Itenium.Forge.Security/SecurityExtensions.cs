using Itenium.Forge.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Itenium.Forge.Security;

/// <summary>
/// Common security extension methods.
/// Use with Itenium.Forge.Security.Keycloak or Itenium.Forge.Security.OpenIddict.
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Registers common security services (ICurrentUser, IHttpContextAccessor) and
    /// applies the authorization configuration from <paramref name="configureAuthorization"/>.
    /// Called automatically by AddForgeKeycloak() or AddForgeOpenIddict().
    /// </summary>
    public static void AddForgeSecurityCore(
        this IServiceCollection services,
        Action<ForgeAuthorizationBuilder>? configureAuthorization = null)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        var options = new ForgeAuthorizationOptions();
        configureAuthorization?.Invoke(new ForgeAuthorizationBuilder(options));
        services.AddSingleton(options);

        services.AddAuthorization(authOptions =>
        {
            // Default to requiring authentication unless explicitly opted out.
            // This also covers the non-Development silent fallback when IsConfigured = false.
            if (options.Mode != ForgeAuthorizationMode.AllowAnonymous)
            {
                authOptions.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            }

            foreach (var (name, configure) in options.Policies)
            {
                authOptions.AddPolicy(name, configure);
            }
        });
    }

    /// <summary>
    /// Adds CORS, authentication, authorization, and user logging middleware to the pipeline.
    /// CORS must be before authentication for preflight requests to work.
    /// Validates the authorization configuration and crashes in Development if it is incomplete.
    /// </summary>
    private const string IncompleteConfigurationMessage =
        "No authorization policy configured. " +
        "Call RequireAuthenticatedByDefault() (with at least one named policy) " +
        "or AllowAnonymousByDefault() on the security builder.";

    public static void UseForgeSecurity(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<ForgeAuthorizationOptions>();
        var isDevelopment = app.Environment.IsDevelopment();
        var isIncomplete = !options.IsConfigured ||
                           (options.Mode == ForgeAuthorizationMode.RequireAuthenticated && options.Policies.Count == 0);

        if (isIncomplete)
        {
            if (isDevelopment)
                throw new InvalidOperationException(IncompleteConfigurationMessage);

            if (!options.IsConfigured)
                app.Logger.LogError(
                    "Forge: No authorization policy configured. Defaulting to RequireAuthenticatedByDefault. " +
                    "Call RequireAuthenticatedByDefault() or AllowAnonymousByDefault() on the security builder.");
        }
        else if (options.Mode == ForgeAuthorizationMode.AllowAnonymous && !isDevelopment)
        {
            app.Logger.LogInformation(
                "Forge: Authorization fallback policy is AllowAnonymous. " +
                "All endpoints are publicly accessible unless individually decorated with [Authorize].");
        }

        var hostSettings = app.Services.GetService<HostingSettings>();
        if (!string.IsNullOrWhiteSpace(hostSettings?.CorsOrigins))
        {
            app.UseCors("CorsPolicy");
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<UserLoggingMiddleware>();
    }
}
