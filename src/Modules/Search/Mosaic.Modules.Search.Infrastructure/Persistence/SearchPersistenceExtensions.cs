using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mosaic.Modules.Search.Infrastructure.Persistence;

internal static class SearchPersistenceExtensions
{
    public static IServiceCollection AddSearchPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Mosaic")
            ?? "Host=localhost;Port=5432;Database=mosaic;Username=mosaic;Password=mosaic";

        services.AddDbContext<SearchDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
