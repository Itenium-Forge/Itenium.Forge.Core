using Itenium.Forge.Controllers;
using Itenium.Forge.ExampleApp;
using Itenium.Forge.Logging;
using Itenium.Forge.Settings;
using Itenium.Forge.Swagger;
using Serilog;

Log.Logger = LoggingExtensions.CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var settings = builder.AddForgeSettings<ExampleSettings>();
    builder.AddForgeLogging();

    builder.AddForgeControllers();
    builder.AddForgeSwagger();
    
    WebApplication app = builder.Build();
    app.UseForgeLogging();

    // app.UseAuthorization();

    app.UseForgeControllers();
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
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
