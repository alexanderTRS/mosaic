using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mosaic.Modules.Content.Infrastructure.Persistence;

internal static class ContentPersistenceExtensions
{
    public static IServiceCollection AddContentPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Mosaic")
            ?? "Host=localhost;Port=5432;Database=mosaic;Username=mosaic;Password=mosaic";

        services.AddDbContext<ContentDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
