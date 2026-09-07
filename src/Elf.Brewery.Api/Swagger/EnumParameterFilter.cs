using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Elf.Brewery.Api.Swagger;

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
