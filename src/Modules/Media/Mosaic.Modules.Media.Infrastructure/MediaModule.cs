using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mosaic.Modules.Abstractions;
using Mosaic.Modules.Media.Application;
using Mosaic.Modules.Media.Application.MediaAssets;
using Mosaic.Modules.Media.Application.Security;
using Mosaic.Modules.Media.Application.Storage;
using Mosaic.Modules.Media.Infrastructure.Persistence;
using Mosaic.Modules.Media.Infrastructure.Security;
using Mosaic.Modules.Media.Infrastructure.Storage;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Media.Infrastructure;

public sealed class MediaModule : IMosaicModule
{
    public string Name => "Media";

    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.Configure<LocalMediaStorageOptions>(options =>
        {
            options.RootPath = configuration["Media:Storage:Local:RootPath"] ?? options.RootPath;
        });
        services.AddScoped<UploadMediaAssetHandler>();
        services.AddScoped<GetMediaAssetHandler>();
        services.AddScoped<ListMediaAssetsHandler>();
        services.AddScoped<OpenMediaAssetFileHandler>();
        services.AddScoped<UpdateMediaAssetMetadataHandler>();
        services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
        services.AddScoped<IMediaStorage, LocalMediaStorage>();
        services.AddScoped<IMediaAccessService, MediaAccessService>();
        services.AddScoped<IMediaUnitOfWork>(provider => provider.GetRequiredService<MediaDbContext>());
        services.AddHostedService<MediaMigrator>();

        services.AddMediaPersistence(configuration);
    }
}
