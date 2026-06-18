using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mosaic.Modules.Abstractions;

public static class ModuleRegistrationExtensions
{
    public static IServiceCollection AddMosaicModule<TModule>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TModule : class, IMosaicModule, new()
    {
        var module = new TModule();
        module.AddServices(services, configuration);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMosaicModule>(module));

        return services;
    }
}
