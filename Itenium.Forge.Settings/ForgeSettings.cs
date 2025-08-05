using Microsoft.Extensions.Configuration;

namespace Itenium.Forge.Settings;

public class ForgeSettings
{
    public string ServiceName { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string Tenant { get; set; } = "";
    public string Environment { get; set; } = "";
    public string Application { get; set; } = "";

    /// <summary>
    /// Reads appsettings.json and appsettings.environment.json
    /// Environment is DOTNET_ENVIRONMENT, defaulting to Development
    /// </summary>
    public static T Load<T>()
        where T : class, IForgeSettings, new()
    {
        string environment = System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
        return Load<T>(environment);
    }

    internal static T Load<T>(string environment)
        where T : class, IForgeSettings, new()
    {
        bool reloadOnChange = environment == "Development";

        var builder = new ConfigurationBuilder()
            .SetBasePath(System.IO.Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange);

        if (!string.IsNullOrEmpty(environment))
        {
            builder.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange);
        }

        IConfigurationRoot config = builder.Build();

        var settings = new T();
        config.Bind(settings);

        if (string.IsNullOrWhiteSpace(settings.Forge.Environment))
            settings.Forge.Environment = environment;
        else if (settings.Forge.Environment != environment)
            throw new Exception($"Environments from $env:DOTNET_ENVIRONMENT ({environment}) and appsettings.{environment}.json ({settings.Forge.Environment}) do not match");

        return settings;
    }

    public override string ToString() => $"{Application} :: {ServiceName} ({Environment}, {Tenant})";
}
