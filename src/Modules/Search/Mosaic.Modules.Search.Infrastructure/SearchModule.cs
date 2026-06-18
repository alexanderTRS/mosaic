using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mosaic.Modules.Abstractions;
using Mosaic.Modules.Search.Application.ContentSearch;
using Mosaic.Modules.Search.Application.Security;
using Mosaic.Modules.Search.Infrastructure.Persistence;
using Mosaic.Modules.Search.Infrastructure.Security;

namespace Mosaic.Modules.Search.Infrastructure;

public sealed class SearchModule : IMosaicModule
{
    public string Name => "Search";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<SearchContentItemsHandler>();
        services.AddScoped<ReindexContentSearchHandler>();
        services.AddScoped<IContentSearchRepository, ContentSearchRepository>();
        services.AddScoped<ISearchAccessService, SearchAccessService>();
        services.AddHostedService<SearchMigrator>();

        services.AddSearchPersistence(configuration);
    }
}
