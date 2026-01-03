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

        // Seed test users
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ForgeUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Create roles
        foreach (var roleName in new[] { "admin", "user" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole(roleName);
                var result = await roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }

        // Create admin user
        var existingAdmin = await userManager.FindByEmailAsync("admin@test.local");
        if (existingAdmin == null)
        {
            var admin = new ForgeUser
            {
                UserName = "admin",
                Email = "admin@test.local",
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User"
            };
            var createResult = await userManager.CreateAsync(admin, "AdminPassword123!");
            if (createResult.Succeeded)
            {
                await userManager.AddToRolesAsync(admin, ["admin", "user"]);
            }
        }

        // Create regular user
        var existingUser = await userManager.FindByEmailAsync("user@test.local");
        if (existingUser == null)
        {
            var user = new ForgeUser
            {
                UserName = "user",
                Email = "user@test.local",
                EmailConfirmed = true,
                FirstName = "Regular",
                LastName = "User"
            };
            var createResult = await userManager.CreateAsync(user, "UserPassword123!");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "user");
            }
        }
    }
}
