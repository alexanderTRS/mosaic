using HotChocolate.Types;
using Mosaic.Modules.Content.Application.ContentItems;

namespace Mosaic.Modules.Content.GraphQL.Dynamic;

public sealed class ContentItemsPageType : ObjectType<ContentItemsPage>
{
    protected override void Configure(IObjectTypeDescriptor<ContentItemsPage> descriptor)
    {
        descriptor.Name("ContentItemsPage");
        descriptor.Field(page => page.Items).Type<NonNullType<ListType<NonNullType<ContentItemDetailsType>>>>();
        descriptor.Field(page => page.TotalCount).Type<NonNullType<IntType>>();
        descriptor.Field(page => page.Skip).Type<NonNullType<IntType>>();
        descriptor.Field(page => page.Take).Type<NonNullType<IntType>>();
    }
}
