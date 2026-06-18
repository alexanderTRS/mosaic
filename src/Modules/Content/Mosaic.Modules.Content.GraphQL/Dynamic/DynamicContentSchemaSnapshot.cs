using Mosaic.Modules.Content.Domain.ContentFields;

namespace Mosaic.Modules.Content.GraphQL.Dynamic;

public sealed record DynamicContentSchemaSnapshot(
    IReadOnlyCollection<DynamicContentTypeDefinition> ContentTypes)
{
    public static DynamicContentSchemaSnapshot Empty { get; } = new([]);
}

public sealed record DynamicContentTypeDefinition(
    Guid Id,
    string ApiName,
    string SingleFieldName,
    string CollectionFieldName,
    string GraphQlTypeName,
    int SchemaVersion,
    IReadOnlyCollection<DynamicContentFieldDefinition> Fields);

public sealed record DynamicContentFieldDefinition(
    string ApiName,
    FieldKind Kind,
    LocalizationMode Localization,
    bool IsRequired,
    bool IsRepeatable,
    bool IsDeprecated,
    string? RelationTargetContentTypeApiName = null);
