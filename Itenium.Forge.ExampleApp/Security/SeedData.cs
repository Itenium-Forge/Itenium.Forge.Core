using System.Security.Claims;
using Itenium.Forge.Security.OpenIddict;
using Microsoft.AspNetCore.Identity;

namespace Itenium.Forge.ExampleApp.Security;

public static class SeedData
{
    /// <summary>
    /// Seeds test users for development.
    /// </summary>
    public static async Task SeedTestUsersAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ForgeUser>>();

        // Create admin user with department claim
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
                // Add custom claim - this will be included in the token
                await userManager.AddClaimAsync(admin, new Claim("department", "IT"));
            }
        }

        // Create regular user with department claim
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
                // Add custom claim - this will be included in the token
                await userManager.AddClaimAsync(user, new Claim("department", "Sales"));
            }
        }
    }
}
