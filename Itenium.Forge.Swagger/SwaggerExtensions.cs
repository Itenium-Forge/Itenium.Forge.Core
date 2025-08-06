using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Itenium.Forge.Swagger;

public static class SwaggerExtensions
{
    public static void AddSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, $"{builder.Environment.ApplicationName}.xml");
            options.IncludeXmlComments(filePath);
        });
    }

    public static void UseSwagger(this WebApplication app)
    {
        app.UseSwagger(options => { });
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", $"{app.Environment.ApplicationName} v1");
        });
    }
}
