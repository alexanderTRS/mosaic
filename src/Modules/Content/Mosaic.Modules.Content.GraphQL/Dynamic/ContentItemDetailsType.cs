using HotChocolate.Types;
using Mosaic.Modules.Content.Application.ContentItems;

namespace Mosaic.Modules.Content.GraphQL.Dynamic;

public sealed class ContentItemDetailsType : ObjectType<ContentItemDetails>
{
    protected override void Configure(IObjectTypeDescriptor<ContentItemDetails> descriptor)
    {
        descriptor.Name("ContentItem");
        descriptor.Field(item => item.Id).Type<NonNullType<UuidType>>();
        descriptor.Field(item => item.ContentTypeId).Type<NonNullType<UuidType>>();
        descriptor.Field(item => item.ContentTypeApiName).Type<NonNullType<StringType>>();
        descriptor.Field(item => item.Status).Type<NonNullType<EnumType<Domain.ContentItems.ContentItemStatus>>>();
        descriptor.Field(item => item.Data).Type<NonNullType<StringType>>();
        descriptor.Field(item => item.CreatedAt).Type<NonNullType<DateTimeType>>();
        descriptor.Field(item => item.UpdatedAt).Type<NonNullType<DateTimeType>>();
    }
}
