using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

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

public sealed class EnumParameterFilter : IParameterFilter
{
    public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
    {
        var enumType = Nullable.GetUnderlyingType(context.ApiParameterDescription.Type)
            ?? context.ApiParameterDescription.Type;

        if (!enumType.IsEnum)
            return;

        var values = Enum.GetValues(enumType);
        var names = Enum.GetNames(enumType);

        parameter.Description = string.Join(", ", values.Cast<object>()
            .Select((value, index) => $"{names[index]} = {Convert.ToInt32(value)}"));

        parameter.Schema.Type = "string";
        parameter.Schema.Format = null;
        parameter.Schema.Enum = names
            .Select(name => (IOpenApiAny)new OpenApiString(name))
            .ToList();
    }
}

public sealed class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var enumType = Nullable.GetUnderlyingType(context.Type) ?? context.Type;

        if (!enumType.IsEnum)
            return;

        schema.Type = "string";
        schema.Format = null;
        schema.Enum = Enum.GetNames(enumType)
            .Select(name => (IOpenApiAny)new OpenApiString(name))
            .ToList();
    }
}