using Itenium.Forge.Controllers;
using Itenium.Forge.Core;
using Itenium.Forge.ExampleApp;
using Itenium.Forge.Logging;
using Itenium.Forge.Settings;
using Itenium.Forge.Swagger;
using Serilog;

Log.Logger = LoggingExtensions.CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var settings = builder.AddForgeSettings<ExampleSettings>();
    builder.AddForgeLogging();

    builder.AddForgeControllers();
    builder.AddForgeSwagger(typeof(ForgeSettings));
    
    WebApplication app = builder.Build();
    app.UseForgeLogging();

    // app.UseAuthorization();

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
