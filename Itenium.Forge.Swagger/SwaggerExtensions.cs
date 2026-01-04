using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Itenium.Forge.Swagger;

public static class SwaggerExtensions
{
    /// <summary>
    /// Configure Swagger with XmlComments.
    /// Project must have: {GenerateDocumentationFile}true{/GenerateDocumentationFile}
    /// </summary>
    /// <param name="builder">The WebApp builder</param>
    /// <param name="typesFromOtherAssemblies">
    /// Provide a type from each assembly that contains a class which is used as input
    /// or output for a controller action method to also load its XmlComments xml file
    /// </param>
    public static void AddForgeSwagger(this WebApplicationBuilder builder, params Type[] typesFromOtherAssemblies)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, $"{builder.Environment.ApplicationName}.xml");
            options.IncludeXmlComments(filePath);

            foreach (var type in typesFromOtherAssemblies)
            {
                var mlFilePath = Path.Combine(AppContext.BaseDirectory, $"{type.Assembly.GetName().Name}.xml");
                options.IncludeXmlComments(mlFilePath);
            }

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "JWT Authorization header using the Bearer scheme. Enter token without 'Bearer ' prefix",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
            });

            options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer"),
                    []
                }
            });
        });
    }

    /// <summary>
    /// Add Swagger UI
    /// </summary>
    public static void UseForgeSwagger(this WebApplication app)
    {
        app.UseSwagger(options => { });
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", $"{app.Environment.ApplicationName} v1");
        });
    }
}
