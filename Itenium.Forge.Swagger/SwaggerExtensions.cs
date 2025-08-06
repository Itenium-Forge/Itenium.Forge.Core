using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Itenium.Forge.Swagger;

public static class SwaggerExtensions
{
    public static void AddSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddSwaggerGen(c =>
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, $"{builder.Environment.ApplicationName}.xml");
            c.IncludeXmlComments(filePath);
        });
    }

    public static void UseSwagger(this WebApplication app)
    {
        app.UseSwagger(options => { });
        app.UseSwaggerUI();
    }
}
