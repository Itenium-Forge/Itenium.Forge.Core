using Itenium.Forge.ExampleApp;
using Itenium.Forge.Logging;
using Itenium.Forge.Settings;
using Serilog;

Log.Logger = LoggingExtensions.CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var settings = builder.LoadConfiguration<ExampleSettings>();
    builder.AddLogging();


    builder.Services.AddControllers();

    WebApplication app = builder.Build();
    app.UseLogging();

    // app.UseAuthorization();

    app.MapControllers();

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
