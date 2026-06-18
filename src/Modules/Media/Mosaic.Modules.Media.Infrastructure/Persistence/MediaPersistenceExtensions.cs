using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mosaic.Modules.Media.Infrastructure.Persistence;

internal static class MediaPersistenceExtensions
{
    public static IServiceCollection AddMediaPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Mosaic")
            ?? "Host=localhost;Port=5432;Database=mosaic;Username=mosaic;Password=mosaic";

        services.AddDbContext<MediaDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
