using System.Text.Json;
using System.Text.Json.Serialization;
using Itenium.Forge.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Itenium.Forge.HealthChecks;

/// <summary>
/// Custom health check response writer that includes ForgeSettings metadata.
/// </summary>
public static class ForgeHealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Writes the health check response as JSON including ForgeSettings metadata.
    /// </summary>
    public static async Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var forgeSettings = context.RequestServices.GetService<ForgeSettings>();

        var response = new HealthCheckResponse
        {
            Status = report.Status.ToString(),
            Service = forgeSettings?.ServiceName,
            Application = forgeSettings?.Application,
            Environment = forgeSettings?.Environment,
            Tenant = forgeSettings?.Tenant,
            Team = forgeSettings?.TeamName,
            Checks = report.Entries.Select(e => new HealthCheckEntry
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration.ToString()
            }).ToList()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private class HealthCheckResponse
    {
        public required string Status { get; init; }
        public string? Service { get; init; }
        public string? Application { get; init; }
        public string? Environment { get; init; }
        public string? Tenant { get; init; }
        public string? Team { get; init; }
        public List<HealthCheckEntry> Checks { get; init; } = [];
    }

    private class HealthCheckEntry
    {
        public required string Name { get; init; }
        public required string Status { get; init; }
        public string? Description { get; init; }
        public required string Duration { get; init; }
    }
}
