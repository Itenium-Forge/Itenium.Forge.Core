using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

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

        // Register common security services
        builder.Services.AddForgeSecurityCore();

        // Add DbContext
        builder.Services.AddDbContext<TContext>(configureDbContext);

        // Add ASP.NET Core Identity
        builder.Services.AddIdentity<ForgeUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<TContext>()
            .AddDefaultTokenProviders();

        // Add OpenIddict
        builder.Services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<TContext>();
            })
            .AddServer(options =>
            {
                // Enable the required endpoints
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
                    .EnableEndSessionEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        // Configure authorization policies
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("admin", policy => policy.RequireRole("admin"));
            options.AddPolicy("user", policy => policy.RequireRole("user"));
        });

        // Store config for seeding
        builder.Services.AddSingleton(config);
    }

    /// <summary>
    /// Seeds the default OpenIddict client and test users.
    /// Call this after app.Build() to ensure the database is ready.
    /// </summary>
    public static async Task SeedOpenIddictDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<OpenIddictConfiguration>();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        // Create the SPA client if it doesn't exist
        var client = await manager.FindByClientIdAsync(config.ClientId);
        if (client == null)
        {
            var redirectUris = config.RedirectUris
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            var postLogoutUris = config.PostLogoutRedirectUris
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = config.ClientId,
                DisplayName = config.ClientDisplayName,
                ClientType = OpenIddictConstants.ClientTypes.Public,
                RedirectUris = { new Uri(redirectUris.First()) },
                PostLogoutRedirectUris = { new Uri(postLogoutUris.First()) },
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.EndSession,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.Password,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles
                }
            });

            // Add additional redirect URIs
            foreach (var uri in redirectUris.Skip(1))
            {
                // Note: OpenIddict 6.x handles multiple URIs differently
                // For now, only first URI is added
            }
        }

        // Seed test users
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ForgeUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Create roles
        foreach (var roleName in new[] { "admin", "user" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Create admin user
        if (await userManager.FindByEmailAsync("admin@test.local") == null)
        {
            var admin = new ForgeUser
            {
                UserName = "admin",
                Email = "admin@test.local",
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User"
            };
            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.AddToRolesAsync(admin, new[] { "admin", "user" });
        }

        // Create regular user
        if (await userManager.FindByEmailAsync("user@test.local") == null)
        {
            var user = new ForgeUser
            {
                UserName = "user",
                Email = "user@test.local",
                EmailConfirmed = true,
                FirstName = "Regular",
                LastName = "User"
            };
            await userManager.CreateAsync(user, "User123!");
            await userManager.AddToRoleAsync(user, "user");
        }
    }
}
