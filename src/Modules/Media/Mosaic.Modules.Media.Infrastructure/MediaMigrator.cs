using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mosaic.Modules.Media.Infrastructure.Persistence;

namespace Mosaic.Modules.Media.Infrastructure;

public sealed class MediaMigrator : IHostedService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<MediaMigrator> logger;

    public MediaMigrator(IServiceScopeFactory scopeFactory, ILogger<MediaMigrator> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MediaDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Media database schema is up to date.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
