using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mosaic.Modules.Search.Infrastructure.Persistence;

public sealed class SearchDbContextFactory : IDesignTimeDbContextFactory<SearchDbContext>
{
    public SearchDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("MOSAIC_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=mosaic;Username=mosaic;Password=mosaic";

        var options = new DbContextOptionsBuilder<SearchDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new SearchDbContext(options);
    }
}
