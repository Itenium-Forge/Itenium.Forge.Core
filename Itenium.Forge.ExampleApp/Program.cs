using Itenium.Forge.Controllers;
using Itenium.Forge.Core;
using Itenium.Forge.ExampleApp;
using Itenium.Forge.ExampleApp.Data;
using Itenium.Forge.ExampleApp.Security;
using Itenium.Forge.Logging;
using Itenium.Forge.Security;
using Itenium.Forge.Security.OpenIddict;
using Itenium.Forge.Settings;
using Itenium.Forge.Swagger;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = LoggingExtensions.CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var settings = builder.AddForgeSettings<ExampleSettings>();
    builder.AddForgeLogging();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.AddForgeOpenIddict<AppDbContext>(options => options.UseSqlite(connectionString));

    builder.AddForgeControllers();
    builder.AddForgeSwagger(typeof(ForgeSettings));

    WebApplication app = builder.Build();

    // Apply migrations and seed data
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
    await app.SeedOpenIddictDataAsync();
    await app.SeedTestUsersAsync();

    app.UseForgeLogging();
    app.UseForgeSecurity();

    app.UseForgeControllers();
    if (app.Environment.IsDevelopment())
    {
        app.UseForgeSwagger();
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
