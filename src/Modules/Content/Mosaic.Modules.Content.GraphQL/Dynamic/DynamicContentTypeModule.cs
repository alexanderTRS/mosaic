using System.Text.Json;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Mosaic.Modules.Content.Application.ContentItems;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.Modules.Content.Domain.ContentItems;

namespace Mosaic.Modules.Content.GraphQL.Dynamic;

public sealed class DynamicContentTypeModule : ITypeModule
{
    private readonly DynamicContentSchemaProvider schemaProvider;

    public DynamicContentTypeModule(DynamicContentSchemaProvider schemaProvider)
    {
        this.schemaProvider = schemaProvider;
    }

    public DynamicContentSchemaSnapshot Snapshot => schemaProvider.Snapshot;

    public event EventHandler<EventArgs>? TypesChanged;

    public ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(
        IDescriptorContext context,
        CancellationToken cancellationToken)
    {
        var types = new List<ITypeSystemMember>();
        var queryTypeDefinition = new ObjectTypeDefinition(OperationTypeNames.Query)
        {
            RuntimeType = typeof(object)
        };

        var contentTypeNames = schemaProvider.Snapshot.ContentTypes
            .ToDictionary(ct => ct.ApiName, ct => ct.GraphQlTypeName);

        foreach (var contentType in schemaProvider.Snapshot.ContentTypes)
        {
            types.Add(CreateObjectType(contentType, contentTypeNames));
            AddQueryFields(queryTypeDefinition, contentType);
        }

        types.Add(ObjectTypeExtension.CreateUnsafe(queryTypeDefinition));

        return ValueTask.FromResult<IReadOnlyCollection<ITypeSystemMember>>(types);
    }

    public void NotifyTypesChanged()
    {
        TypesChanged?.Invoke(this, EventArgs.Empty);
    }

    private static ObjectType CreateObjectType(
        DynamicContentTypeDefinition contentType,
        IReadOnlyDictionary<string, string> contentTypeNames)
    {
        var definition = new ObjectTypeDefinition(contentType.GraphQlTypeName)
        {
            Description = $"Generated object type for content type '{contentType.ApiName}' schema version {contentType.SchemaVersion}.",
            RuntimeType = typeof(ContentItemDetails)
        };

        definition.Fields.Add(new ObjectFieldDefinition(
            "id",
            type: TypeReference.Parse("UUID!"),
            pureResolver: context => context.Parent<ContentItemDetails>().Id));
        definition.Fields.Add(new ObjectFieldDefinition(
            "contentTypeId",
            type: TypeReference.Parse("UUID!"),
            pureResolver: context => context.Parent<ContentItemDetails>().ContentTypeId));
        definition.Fields.Add(new ObjectFieldDefinition(
            "contentTypeApiName",
            type: TypeReference.Parse("String!"),
            pureResolver: context => context.Parent<ContentItemDetails>().ContentTypeApiName));
        definition.Fields.Add(new ObjectFieldDefinition(
            "status",
            type: TypeReference.Parse("ContentItemStatus!"),
            pureResolver: context => context.Parent<ContentItemDetails>().Status));
        definition.Fields.Add(new ObjectFieldDefinition(
            "data",
            type: TypeReference.Parse("String!"),
            pureResolver: context => context.Parent<ContentItemDetails>().Data));
        definition.Fields.Add(new ObjectFieldDefinition(
            "createdAt",
            type: TypeReference.Parse("DateTime!"),
            pureResolver: context => context.Parent<ContentItemDetails>().CreatedAt));
        definition.Fields.Add(new ObjectFieldDefinition(
            "updatedAt",
            type: TypeReference.Parse("DateTime!"),
            pureResolver: context => context.Parent<ContentItemDetails>().UpdatedAt));

        foreach (var field in contentType.Fields.Where(field => !field.IsDeprecated))
        {
            if (field.Kind == FieldKind.Relation
                && !string.IsNullOrWhiteSpace(field.RelationTargetContentTypeApiName)
                && contentTypeNames.TryGetValue(field.RelationTargetContentTypeApiName, out var targetGraphQlTypeName))
            {
                var isRepeatable = field.IsRepeatable;
                var typeName = isRepeatable ? $"[{targetGraphQlTypeName}!]" : $"{targetGraphQlTypeName}";
                var fieldName = field.ApiName;

                definition.Fields.Add(new ObjectFieldDefinition(
                    fieldName,
                    type: TypeReference.Parse(typeName),
                    resolver: context => ResolveRelation(context.Parent<ContentItemDetails>(), fieldName, isRepeatable, context)));
            }
            else
            {
                definition.Fields.Add(new ObjectFieldDefinition(
                    field.ApiName,
                    type: TypeReference.Parse(OutputTypeName(field)),
                    pureResolver: context => ResolveFieldValue(context.Parent<ContentItemDetails>(), field)));
            }
        }

        return ObjectType.CreateUnsafe(definition);
    }

    private static void AddQueryFields(
        ObjectTypeDefinition queryTypeDefinition,
        DynamicContentTypeDefinition contentType)
    {
        queryTypeDefinition.Fields.Add(new ObjectFieldDefinition(
            contentType.SingleFieldName,
            $"Dynamic single-item query for published content type '{contentType.ApiName}'.",
            TypeReference.Parse(contentType.GraphQlTypeName),
            resolver: context => ResolveSingle(context, contentType.ApiName))
        {
            Arguments =
            {
                new ArgumentDefinition("id", type: TypeReference.Parse("UUID!"))
            }
        });

        queryTypeDefinition.Fields.Add(new ObjectFieldDefinition(
            contentType.CollectionFieldName,
            $"Dynamic collection query for published content type '{contentType.ApiName}'.",
            TypeReference.Parse($"[{contentType.GraphQlTypeName}!]!"),
            resolver: context => ResolveCollection(context, contentType.ApiName))
        {
            Arguments =
            {
                new ArgumentDefinition("status", type: TypeReference.Parse("ContentItemStatus")),
                new ArgumentDefinition("search", type: TypeReference.Parse("String")),
                new ArgumentDefinition("orderBy", type: TypeReference.Parse("String")),
                new ArgumentDefinition("descending", type: TypeReference.Parse("Boolean")),
                new ArgumentDefinition("skip", type: TypeReference.Parse("Int")),
                new ArgumentDefinition("take", type: TypeReference.Parse("Int"))
            }
        });
    }

    private static async ValueTask<object?> ResolveSingle(
        IResolverContext context,
        string contentTypeApiName)
    {
        var id = context.ArgumentValue<Guid>("id");
        var handler = context.Service<GetContentItemHandler>();
        var item = await handler.Handle(new ContentItemId(id), context.RequestAborted);

        return item.ContentTypeApiName == contentTypeApiName ? item : null;
    }

    private static async ValueTask<object?> ResolveCollection(
        IResolverContext context,
        string contentTypeApiName)
    {
        var handler = context.Service<ListContentItemsHandler>();
        var status = context.ArgumentValue<ContentItemStatus?>("status");
        var search = context.ArgumentValue<string?>("search");
        var orderBy = context.ArgumentValue<string?>("orderBy");
        var descending = context.ArgumentValue<bool?>("descending") ?? true;
        var skip = context.ArgumentValue<int?>("skip") ?? 0;
        var take = context.ArgumentValue<int?>("take") ?? 20;

        var page = await handler.Handle(
            new ListContentItemsQuery(
                contentTypeApiName,
                status,
                search,
                orderBy,
                descending,
                skip,
                take),
            context.RequestAborted);

        return page.Items;
    }

    private static async ValueTask<object?> ResolveRelation(
        ContentItemDetails item,
        string fieldName,
        bool isRepeatable,
        IResolverContext context)
    {
        using var document = JsonDocument.Parse(item.Data);
        if (!document.RootElement.TryGetProperty(fieldName, out var value)
            || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return isRepeatable ? Array.Empty<ContentItemDetails>() : null;
        }

        var handler = context.Service<GetContentItemHandler>();

        if (isRepeatable && value.ValueKind == JsonValueKind.Array)
        {
            var results = new List<ContentItemDetails>();
            foreach (var element in value.EnumerateArray())
            {
                var id = TryExtractRelationId(element);
                if (id.HasValue)
                {
                    try
                    {
                        var resolved = await handler.Handle(new ContentItemId(id.Value), context.RequestAborted);
                        if (resolved is not null) results.Add(resolved);
                    }
                    catch
                    {
                        // skip unresolved references
                    }
                }
            }

            return results;
        }

        var singleId = TryExtractRelationId(value);
        if (!singleId.HasValue) return isRepeatable ? Array.Empty<ContentItemDetails>() : null;

        try
        {
            return await handler.Handle(new ContentItemId(singleId.Value), context.RequestAborted);
        }
        catch
        {
            return isRepeatable ? Array.Empty<ContentItemDetails>() : null;
        }
    }

    private static Guid? TryExtractRelationId(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var id))
        {
            return id;
        }

        if (value.ValueKind == JsonValueKind.Object
            && value.TryGetProperty("id", out var idProp)
            && idProp.ValueKind == JsonValueKind.String
            && Guid.TryParse(idProp.GetString(), out var objId))
        {
            return objId;
        }

        return null;
    }

    private static string OutputTypeName(DynamicContentFieldDefinition field)
    {
        if (field.Localization == LocalizationMode.Localized || field.IsRepeatable)
        {
            return "String";
        }

        return field.Kind switch
        {
            FieldKind.Boolean => "Boolean",
            FieldKind.Integer => "Long",
            FieldKind.Decimal => "Decimal",
            FieldKind.DateTime => "DateTime",
            _ => "String"
        };
    }

    private static object? ResolveFieldValue(
        ContentItemDetails item,
        DynamicContentFieldDefinition field)
    {
        using var document = JsonDocument.Parse(item.Data);
        if (!document.RootElement.TryGetProperty(field.ApiName, out var value)
            || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (field.Localization == LocalizationMode.Localized || field.IsRepeatable)
        {
            return value.GetRawText();
        }

        return field.Kind switch
        {
            FieldKind.Boolean => value.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? value.GetBoolean()
                : null,
            FieldKind.Integer => value.TryGetInt64(out var integer) ? integer : null,
            FieldKind.Decimal => value.TryGetDecimal(out var number) ? number : null,
            FieldKind.DateTime => value.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(value.GetString(), out var dateTime)
                    ? dateTime
                    : null,
            FieldKind.String or FieldKind.Text => value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : value.GetRawText(),
            _ => value.GetRawText()
        };
    }
}
