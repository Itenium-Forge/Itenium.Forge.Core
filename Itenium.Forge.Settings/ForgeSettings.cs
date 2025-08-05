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
        return settings;
    }

    public override string ToString() => $"{Application} :: {ServiceName} ({Environment}, {Tenant})";
}
