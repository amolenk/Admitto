using System.Collections.Concurrent;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Amolenk.Admitto.ApiService.OpenApi;

public sealed class NumberTypeTransformer : IOpenApiSchemaTransformer
{
    private static readonly ConcurrentDictionary<Type, (JsonSchemaType Type, string? Format)> TypeMappings = new();

    public static void MapType<T>(OpenApiSchema schema)
    {
        TypeMappings[typeof(T)] = (schema.Type ?? JsonSchemaType.Null, schema.Format);
    }

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var clrType = context.JsonTypeInfo.Type;

        if (TypeMappings.TryGetValue(clrType, out var mapping))
        {
            schema.Type = mapping.Type;
            schema.Format = mapping.Format;
        }

        return Task.CompletedTask;
    }
}