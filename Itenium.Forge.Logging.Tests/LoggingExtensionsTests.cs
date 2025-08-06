using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Itenium.Forge.Logging.Tests;

public class LoggingExtensionsTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddLogging();
        var app = builder.Build();
        // app.UseSe

        var logger = app.Services.GetRequiredService<ILogger<LoggingExtensionsTests>>();
        logger.LogInformation("This is a {Type} event", "test");
    }
}
