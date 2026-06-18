using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mosaic.Modules.Abstractions;

public interface IMosaicModule
{
    string Name { get; }

    void AddServices(IServiceCollection services, IConfiguration configuration);
}
