using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Itenium.Forge.Security.OpenIddict;

/// <summary>
/// DbContext for ASP.NET Core Identity and OpenIddict.
/// Inherit from this class to add your own entities.
/// </summary>
public class ForgeIdentityDbContext : IdentityDbContext<ForgeUser>
{
    public ForgeIdentityDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ForgeUser>().ToTable("Users");
        builder.UseOpenIddict();
    }
}
