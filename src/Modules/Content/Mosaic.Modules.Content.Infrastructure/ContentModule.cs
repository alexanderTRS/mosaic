using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mosaic.Modules.Abstractions;
using Mosaic.Modules.Content.Application;
using Mosaic.Modules.Content.Application.ContentItems;
using Mosaic.Modules.Content.Application.ContentTypes;
using Mosaic.Modules.Content.Infrastructure.Persistence;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Content.Infrastructure;

public sealed class ContentModule : IMosaicModule
{
    public string Name => "Content";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.TryAddScoped<IContentSchemaChangeNotifier, NoOpContentSchemaChangeNotifier>();
        services.AddScoped<CreateContentTypeHandler>();
        services.AddScoped<AddContentFieldHandler>();
        services.AddScoped<DeprecateContentFieldHandler>();
        services.AddScoped<PublishContentTypeHandler>();
        services.AddScoped<ListContentTypesHandler>();
        services.AddScoped<ContentItemMutationService>();
        services.AddScoped<CreateContentItemHandler>();
        services.AddScoped<UpdateContentItemHandler>();
        services.AddScoped<ArchiveContentItemHandler>();
        services.AddScoped<PublishContentItemHandler>();
        services.AddScoped<UnpublishContentItemHandler>();
        services.AddScoped<GetContentItemHandler>();
        services.AddScoped<ListContentItemsHandler>();
        services.AddScoped<ListContentItemVersionsHandler>();
        services.AddScoped<IContentTypeRepository, ContentTypeRepository>();
        services.AddScoped<IContentItemRepository, ContentItemRepository>();
        services.AddScoped<IContentUnitOfWork>(provider => provider.GetRequiredService<ContentDbContext>());

        services.AddContentPersistence(configuration);
    }
}
