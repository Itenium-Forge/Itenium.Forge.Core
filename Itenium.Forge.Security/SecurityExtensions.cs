using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Itenium.Forge.Security;

/// <summary>
/// Extension methods for configuring Keycloak-based JWT authentication.
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Adds JWT Bearer authentication configured for Keycloak.
    /// Reads configuration from ForgeConfiguration:Security section.
    /// </summary>
    public static void AddForgeSecurity(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration
            .GetSection("ForgeConfiguration:Security")
            .Get<SecurityConfiguration>();

        if (config == null || string.IsNullOrEmpty(config.Authority))
        {
            throw new InvalidOperationException(
                "Security configuration is missing. Add ForgeConfiguration:Security section to appsettings.json with Authority and Audience.");
        }

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();
        builder.Services.AddSingleton<IClaimsTransformation, KeycloakClaimsTransformer>();

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = config.Authority;
                options.Audience = config.Audience;
                options.RequireHttpsMetadata = config.RequireHttpsMetadata;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = !string.IsNullOrEmpty(config.Audience),
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config.Authority,
                    ValidAudience = config.Audience,
                    // Keycloak uses 'preferred_username' for the name claim
                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles"
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("admin", policy => policy.RequireRole("admin"));
            options.AddPolicy("user", policy => policy.RequireRole("user"));
        });
    }

    /// <summary>
    /// Adds authentication and authorization middleware to the pipeline.
    /// Also adds user context enrichment for logging.
    /// </summary>
    public static void UseForgeSecurity(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<UserLoggingMiddleware>();
    }
}
