using Itenium.Forge.ExampleApp;
using Itenium.Forge.Logging;
using Itenium.Forge.Settings;
using Itenium.Forge.Swagger;
using Serilog;

Log.Logger = LoggingExtensions.CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var settings = builder.LoadConfiguration<ExampleSettings>();
    builder.AddLogging();

    builder.Services.AddControllers();
    builder.AddSwagger();

    WebApplication app = builder.Build();
    app.UseLogging();

    // app.UseAuthorization();

    app.MapControllers();
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
