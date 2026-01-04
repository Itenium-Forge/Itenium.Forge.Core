using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

namespace Itenium.Forge.Controllers;

public static class ControllerExtensions
{
    public static void AddForgeControllers(this WebApplicationBuilder builder)
    {
        var hostSettings = builder.Configuration.GetSection("Hosting").Get<HostingSettings>();
        if (hostSettings != null)
        {
            builder.Services.AddSingleton(hostSettings);
        }

        builder.Services.AddControllers().AddControllersAsServices().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.WriteIndented = false;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });

        if (!string.IsNullOrWhiteSpace(hostSettings?.CorsOrigins))
        {
            var securityEnabled = builder.Configuration.GetSection("ForgeConfiguration:Security").Exists();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", corsBuilder =>
                {
                    var origins = hostSettings.CorsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    corsBuilder
                        .WithOrigins(origins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();

                    if (securityEnabled)
                    {
                        corsBuilder.AllowCredentials();
                    }
                });
            });
        }

        if (hostSettings != null && hostSettings.AllowedHosts.Any())
        {
            builder.Services.Configure<HostFilteringOptions>(options =>
            {
                options.AllowedHosts = hostSettings.AllowedHosts;
            });
        }
    }

    public static void UseForgeControllers(this WebApplication app)
    {
        app.MapControllers();
    }
}
