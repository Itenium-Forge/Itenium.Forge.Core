using Itenium.Forge.Security.OpenIddict;
using Microsoft.EntityFrameworkCore;

namespace Itenium.Forge.ExampleApp.Data;

public class AppDbContext : ForgeIdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}
