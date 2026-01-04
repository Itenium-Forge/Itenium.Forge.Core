using Itenium.Forge.Controllers;
using Itenium.Forge.Core;
using Itenium.Forge.ExampleApp;
using Itenium.Forge.ExampleApp.Data;
using Itenium.Forge.ExampleApp.Security;
using Itenium.Forge.HealthChecks;
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

    builder.Services.AddScoped<IExampleAppUser, ExampleAppUser>();

    builder.AddForgeControllers();
    builder.AddForgeProblemDetails();
    builder.AddForgeSwagger(typeof(ForgeSettings));
    builder.AddForgeHealthChecks();

    WebApplication app = builder.Build();

    // Apply migrations and seed data
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
    await app.SeedOpenIddictDataAsync();
    await app.SeedTestUsersAsync();

    app.UseForgeProblemDetails();
    app.UseForgeLogging();
    app.UseForgeSecurity();

    app.UseForgeControllers();
    if (app.Environment.IsDevelopment())
    {
        app.UseForgeSwagger();
    }

    app.UseForgeHealthChecks();

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

// Make Program class accessible for WebApplicationFactory in tests
public partial class Program { }
