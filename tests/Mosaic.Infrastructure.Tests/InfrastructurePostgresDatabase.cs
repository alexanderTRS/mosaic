using Microsoft.EntityFrameworkCore;
using Mosaic.Modules.Content.Infrastructure.Persistence;
using Mosaic.Modules.Identity.Infrastructure.Persistence;
using Mosaic.Modules.Media.Infrastructure.Persistence;
using Mosaic.Modules.Search.Infrastructure.Persistence;
using Npgsql;

namespace Mosaic.Infrastructure.Tests;

internal sealed class InfrastructurePostgresDatabase : IAsyncDisposable
{
    private readonly string databaseName = $"mosaic_infrastructure_{Guid.NewGuid():N}";

    private string ConnectionString =>
        $"Host=localhost;Port=5432;Database={databaseName};Username=mosaic;Password=mosaic";

    private static string AdminConnectionString =>
        "Host=localhost;Port=5432;Database=postgres;Username=mosaic;Password=mosaic";

    public async Task Initialize()
    {
        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = $"""CREATE DATABASE "{databaseName}";""";
            await command.ExecuteNonQueryAsync();
        }

        await using (var contentDbContext = CreateContentDbContext())
        {
            await contentDbContext.Database.MigrateAsync();
        }

        await using (var identityDbContext = CreateIdentityDbContext())
        {
            await identityDbContext.Database.MigrateAsync();
        }

        await using (var mediaDbContext = CreateMediaDbContext())
        {
            await mediaDbContext.Database.MigrateAsync();
        }

        await using (var searchDbContext = CreateSearchDbContext())
        {
            await searchDbContext.Database.MigrateAsync();
        }
    }

    public ContentDbContext CreateContentDbContext()
    {
        var options = new DbContextOptionsBuilder<ContentDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new ContentDbContext(options);
    }

    public IdentityDbContext CreateIdentityDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new IdentityDbContext(options);
    }

    public MediaDbContext CreateMediaDbContext()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new MediaDbContext(options);
    }

    public SearchDbContext CreateSearchDbContext()
    {
        var options = new DbContextOptionsBuilder<SearchDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new SearchDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync();

        await using (var terminateCommand = connection.CreateCommand())
        {
            terminateCommand.CommandText = """
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = @databaseName;
                """;
            terminateCommand.Parameters.AddWithValue("databaseName", databaseName);
            await terminateCommand.ExecuteNonQueryAsync();
        }

        await using (var dropCommand = connection.CreateCommand())
        {
            dropCommand.CommandText = $"""DROP DATABASE IF EXISTS "{databaseName}";""";
            await dropCommand.ExecuteNonQueryAsync();
        }
    }
}
