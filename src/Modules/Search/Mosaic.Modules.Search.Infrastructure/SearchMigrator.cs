using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mosaic.Modules.Search.Infrastructure.Persistence;

namespace Mosaic.Modules.Search.Infrastructure;

public sealed class SearchMigrator : IHostedService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<SearchMigrator> logger;

    public SearchMigrator(IServiceScopeFactory scopeFactory, ILogger<SearchMigrator> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SearchDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Search database schema is up to date.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
