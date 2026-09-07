using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Elf.Brewery.Api.Swagger;

public static class SwaggerSetup
{
    public static void Configure(SwaggerGenOptions o)
    {
        o.SwaggerDoc("v1", new OpenApiInfo { Title = "Brewery API", Version = "v1" });
        o.SwaggerDoc("v2", new OpenApiInfo { Title = "Brewery API", Version = "v2" });
        o.ParameterFilter<EnumParameterFilter>();
        o.SchemaFilter<EnumSchemaFilter>();

        o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste only the token from POST /api/auth/token (no 'Bearer ' prefix)"
        });

        o.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        var xml = Path.Combine(AppContext.BaseDirectory,
            $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");

        if (File.Exists(xml))
            o.IncludeXmlComments(xml);
    }
}
