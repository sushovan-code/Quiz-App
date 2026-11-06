using System.Reflection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BidQuiz.Api.OpenApi;

/// <summary>
/// Every property of a contract type is always written, null or not, so each one is required, and
/// the ones declared nullable in C# may be null. This keeps the generated TypeScript types exact.
/// </summary>
public sealed class AllPropertiesRequiredSchemaTransformer : IOpenApiSchemaTransformer
{
    private readonly NullabilityInfoContext _nullability = new();

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (schema.Properties is { Count: > 0 } properties)
        {
            schema.Required = new SortedSet<string>(properties.Keys, StringComparer.Ordinal);
        }

        if (context.JsonPropertyInfo?.AttributeProvider is PropertyInfo property
            && schema.Type is { } type
            && !type.HasFlag(JsonSchemaType.Null)
            && IsNullable(property))
        {
            schema.Type = type | JsonSchemaType.Null;
        }

        return Task.CompletedTask;
    }

    private bool IsNullable(PropertyInfo property)
    {
        lock (_nullability)
        {
            return _nullability.Create(property).ReadState == NullabilityState.Nullable;
        }
    }
}
