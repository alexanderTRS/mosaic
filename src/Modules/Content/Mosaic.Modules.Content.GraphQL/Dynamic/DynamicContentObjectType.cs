using System.Text.Json;
using HotChocolate.Types;
using Mosaic.Modules.Content.Application.ContentItems;
using Mosaic.Modules.Content.Domain.ContentFields;

namespace Mosaic.Modules.Content.GraphQL.Dynamic;

public sealed class DynamicContentObjectType : ObjectType<ContentItemDetails>
{
    private readonly DynamicContentTypeDefinition contentType;

    public DynamicContentObjectType(DynamicContentTypeDefinition contentType)
    {
        this.contentType = contentType;
    }

    protected override void Configure(IObjectTypeDescriptor<ContentItemDetails> descriptor)
    {
        descriptor.Name(contentType.GraphQlTypeName);
        descriptor.Description($"Generated object type for content type '{contentType.ApiName}' schema version {contentType.SchemaVersion}.");

        descriptor.Field(item => item.Id).Type<NonNullType<UuidType>>();
        descriptor.Field(item => item.ContentTypeId).Type<NonNullType<UuidType>>();
        descriptor.Field(item => item.ContentTypeApiName).Type<NonNullType<StringType>>();
        descriptor.Field(item => item.Status).Type<NonNullType<EnumType<Domain.ContentItems.ContentItemStatus>>>();
        descriptor.Field(item => item.Data).Type<NonNullType<StringType>>();
        descriptor.Field(item => item.CreatedAt).Type<NonNullType<DateTimeType>>();
        descriptor.Field(item => item.UpdatedAt).Type<NonNullType<DateTimeType>>();

        foreach (var field in contentType.Fields.Where(field => !field.IsDeprecated))
        {
            var fieldDescriptor = descriptor
                .Field(field.ApiName)
                .Resolve(context => ResolveFieldValue(context.Parent<ContentItemDetails>(), field));
            ApplyOutputType(fieldDescriptor, field);
        }
    }

    private static void ApplyOutputType(
        IObjectFieldDescriptor fieldDescriptor,
        DynamicContentFieldDefinition field)
    {
        if (field.Localization == LocalizationMode.Localized)
        {
            fieldDescriptor.Type<StringType>();
            return;
        }

        if (field.IsRepeatable)
        {
            fieldDescriptor.Type<StringType>();
            return;
        }

        switch (field.Kind)
        {
            case FieldKind.Boolean:
                fieldDescriptor.Type<BooleanType>();
                break;
            case FieldKind.Integer:
                fieldDescriptor.Type<LongType>();
                break;
            case FieldKind.Decimal:
                fieldDescriptor.Type<DecimalType>();
                break;
            case FieldKind.DateTime:
                fieldDescriptor.Type<DateTimeType>();
                break;
            default:
                fieldDescriptor.Type<StringType>();
                break;
        }
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
