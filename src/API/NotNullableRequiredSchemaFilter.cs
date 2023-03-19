namespace API;

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

// https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/2036#issuecomment-804619789
public class NotNullableRequiredSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null)
        {
            return;
        }

        var notNullableProperties = schema.Properties
            .Where(x => !x.Value.Nullable && !schema.Required.Contains(x.Key))
            .ToList();

        foreach (var property in notNullableProperties)
        {
            _ = schema.Required.Add(property.Key);
        }
    }
}
