using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Mosaic.Infrastructure.Tests;

public sealed class SearchInfrastructureTests
{
    [Fact]
    public async Task SearchMigrationsShouldCreateContentItemDocumentsTable()
    {
        await using var database = new InfrastructurePostgresDatabase();
        await database.Initialize();

        await using var dbContext = database.CreateSearchDbContext();
        var searchTables = await dbContext.Database
            .SqlQueryRaw<int>(
                """
                SELECT COUNT(*)::int AS "Value"
                FROM information_schema.tables
                WHERE table_schema = 'search'
                    AND table_name = 'content_item_documents'
                """)
            .SingleAsync();

        searchTables.Should().Be(1);
    }
}
