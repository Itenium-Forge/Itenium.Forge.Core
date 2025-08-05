using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Itenium.Forge.Settings;

public static class SettingsExtensions
{
    /// <summary>
    /// Reads appsettings.json and appsettings.environment.json
    /// - Environment is DOTNET_ENVIRONMENT, defaulting to Development
    /// - Updates the builder.Configuration
    /// - Adds TAppSettings and <see cref="ForgeSettings"/> as singleton service
    /// </summary>
    public static TAppSettings LoadConfiguration<TAppSettings>(this WebApplicationBuilder builder)
        where TAppSettings : class, IForgeSettings, new()
    {
        string environment = System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
        return LoadConfiguration<TAppSettings>(builder, environment);
    }

    internal static T LoadConfiguration<T>(this WebApplicationBuilder builder, string environment)
        where T : class, IForgeSettings, new()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(System.IO.Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

        if (!string.IsNullOrEmpty(environment))
        {
            configurationBuilder.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);
        }

        IConfigurationRoot config = configurationBuilder.Build();

        var settings = new T();
        config.Bind(settings);

        if (string.IsNullOrWhiteSpace(settings.Forge.Environment))
            settings.Forge.Environment = environment;
        else if (settings.Forge.Environment != environment)
            throw new Exception($"Environments from $env:DOTNET_ENVIRONMENT ({environment}) and appsettings.{environment}.json ({settings.Forge.Environment}) do not match");

        builder.Configuration.Sources.Clear();
        builder.Configuration.AddConfiguration(config);

        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(settings.Forge);

        builder.Environment.EnvironmentName = environment;

        return settings;
    }
}
