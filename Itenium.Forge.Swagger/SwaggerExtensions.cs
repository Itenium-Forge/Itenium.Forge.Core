using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Itenium.Forge.Swagger;

public static class SwaggerExtensions
{
    /// <summary>
    /// Configure Swagger with XmlComments.
    /// Project must have: {GenerateDocumentationFile}true{/GenerateDocumentationFile}
    /// </summary>
    public static void AddSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, $"{builder.Environment.ApplicationName}.xml");
            options.IncludeXmlComments(filePath);

            // TODO: This is part of Itenium.Forge.Security
            //options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            //{
            //    Name = "Authorization",
            //    Type = SecuritySchemeType.Http,
            //    Scheme = "Bearer",
            //    In = ParameterLocation.Header,
            //});

            //options.AddSecurityRequirement(new OpenApiSecurityRequirement
            //{
            //    {
            //        new OpenApiSecurityScheme
            //        {
            //            Reference = new OpenApiReference
            //            {
            //                Type = ReferenceType.SecurityScheme,
            //                Id = "Bearer"
            //            }
            //        },
            //        []
            //    }
            //});
        });
    }

    /// <summary>
    /// Add Swagger UI
    /// </summary>
    public static void UseSwagger(this WebApplication app)
    {
        app.UseSwagger(options => { });
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", $"{app.Environment.ApplicationName} v1");
        });
    }
}
