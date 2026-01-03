using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

namespace Itenium.Forge.Security.OpenIddict;

/// <summary>
/// Extension methods for configuring OpenIddict identity server.
/// </summary>
public static class OpenIddictExtensions
{
    /// <summary>
    /// Adds OpenIddict identity server with ASP.NET Core Identity.
    /// </summary>
    /// <typeparam name="TContext">Your DbContext type (must inherit from ForgeIdentityDbContext)</typeparam>
    /// <param name="builder">The WebApplicationBuilder</param>
    /// <param name="configureDbContext">Action to configure the DbContext (e.g., connection string)</param>
    public static void AddForgeOpenIddict<TContext>(
        this WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder> configureDbContext)
        where TContext : ForgeIdentityDbContext
    {
        var config = builder.Configuration
            .GetSection("ForgeConfiguration:Security")
            .Get<OpenIddictConfiguration>() ?? new OpenIddictConfiguration();

        builder.Services.AddForgeSecurityCore();
        builder.Services.AddDbContext<TContext>(configureDbContext);

        builder.Services.AddIdentity<ForgeUser, IdentityRole>(options =>
            {
                //options.Password.RequireDigit = true;
                //options.Password.RequireLowercase = true;
                //options.Password.RequireUppercase = true;
                //options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 16;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<TContext>()
            .AddDefaultTokenProviders();

        builder.Services
            .AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore().UseDbContext<TContext>();
            })
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("/connect/authorize")
                    .SetTokenEndpointUris("/connect/token")
                    .SetUserInfoEndpointUris("/connect/userinfo")
                    .SetEndSessionEndpointUris("/connect/logout");

                // Enable the authorization code flow with PKCE (for SPAs)
                options.AllowAuthorizationCodeFlow()
                    .RequireProofKeyForCodeExchange();

                // Enable the password flow (for testing/simple apps)
                options.AllowPasswordFlow();

                // Enable refresh tokens
                options.AllowRefreshTokenFlow();

                // Set token lifetimes
                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(config.AccessTokenLifetimeMinutes));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(config.RefreshTokenLifetimeDays));

                // Register signing and encryption credentials
                // In production, use proper certificates
                options.AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

                // Register the ASP.NET Core host
                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough()
                    .DisableTransportSecurityRequirement(); // Allow HTTP in development
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        // Set OpenIddict validation as the default authentication scheme for API endpoints
        builder.Services.Configure<AuthenticationOptions>(options =>
        {
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        // Configure authorization policies
        builder.Services.AddAuthorization(options =>
        {
            // Role-based policies
            options.AddPolicy("admin", policy => policy.RequireRole("admin"));
            options.AddPolicy("user", policy => policy.RequireRole("user"));

            // Capability-based policies (one policy per capability)
            foreach (var capability in Enum.GetValues<Capability>())
            {
                options.AddPolicy(capability.ToString(), policy =>
                    policy.RequireClaim("capability", capability.ToString()));
            }
        });

        // Store config for seeding
        builder.Services.AddSingleton(config);
    }

    /// <summary>
    /// Seeds OpenIddict scopes, client application, roles and capabilities.
    /// Call this after app.Build() to ensure the database is ready.
    /// </summary>
    public static async Task SeedOpenIddictDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<OpenIddictConfiguration>();
        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

        // Register scopes
        var scopes = new[] { "openid", "profile", "email", "roles" };
        foreach (var scopeName in scopes)
        {
            if (await scopeManager.FindByNameAsync(scopeName) == null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = scopeName,
                    DisplayName = scopeName.Substring(0, 1).ToUpper() + scopeName.Substring(1),
                    Resources = { "forge-api" }
                });
            }
        }

        // Create the SPA client if it doesn't exist
        var client = await applicationManager.FindByClientIdAsync(config.ClientId);
        if (client == null)
        {
            var redirectUris = config.RedirectUris
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            var postLogoutUris = config.PostLogoutRedirectUris
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = config.ClientId,
                DisplayName = config.ClientDisplayName,
                ClientType = OpenIddictConstants.ClientTypes.Public,
                RedirectUris = { new Uri(redirectUris.First()) },
                PostLogoutRedirectUris = { new Uri(postLogoutUris.First()) },
                Permissions =
                {
                    // Endpoints
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.EndSession,
                    // Grant types
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.Password,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    // Response types
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    // Scopes
                    OpenIddictConstants.Permissions.Prefixes.Scope + "openid",
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "roles",
                }
            });

            // Add additional redirect URIs
            foreach (var uri in redirectUris.Skip(1))
            {
                // Note: OpenIddict 6.x handles multiple URIs differently
                // For now, only first URI is added
            }
        }

        // Seed roles and their capabilities
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var (roleName, capabilities) in config.RoleCapabilities)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                role = new IdentityRole(roleName);
                var result = await roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }

            // Add capability claims to the role
            var existingClaims = await roleManager.GetClaimsAsync(role);
            foreach (var capability in capabilities)
            {
                if (!existingClaims.Any(c => c.Type == "capability" && c.Value == capability))
                {
                    await roleManager.AddClaimAsync(role, new Claim("capability", capability));
                }
            }
        }
    }
}
