using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mosaic.Modules.Media.Infrastructure.Persistence;

namespace Mosaic.Infrastructure.Tests;

public sealed class MediaInfrastructureTests
{
    [Fact]
    public async Task MediaMigrationsShouldCreateAssetsTable()
    {
        await using var database = new InfrastructurePostgresDatabase();
        await database.Initialize();

        await using var dbContext = database.CreateMediaDbContext();
        var mediaTables = await dbContext.Database
            .SqlQueryRaw<int>(
                """
                SELECT COUNT(*)::int AS "Value"
                FROM information_schema.tables
                WHERE table_schema = 'media'
                    AND table_name = 'assets'
                """)
            .SingleAsync();

        mediaTables.Should().Be(1);
    }

    [Fact]
    public async Task MediaRepositoryShouldPersistAssets()
    {
        await using var database = new InfrastructurePostgresDatabase();
        await database.Initialize();

        var assetId = Guid.NewGuid();
        await using (var dbContext = database.CreateMediaDbContext())
        {
            await dbContext.Assets.AddAsync(
                new MediaAssetRecord
                {
                    Id = assetId,
                    FileName = "product.jpg",
                    ContentType = "image/jpeg",
                    Size = 42,
                    StoragePath = "ab/product.jpg",
                    PublicUrl = "/media/assets/file",
                    Width = 100,
                    Height = 200,
                    AltText = "Product",
                    LocalizedAltText = """{"ru-ru":"Товар"}""",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = database.CreateMediaDbContext())
        {
            var asset = await dbContext.Assets.SingleAsync(item => item.Id == assetId);

            asset.FileName.Should().Be("product.jpg");
            asset.LocalizedAltText.Should().Contain("ru-ru");
        }
    }
}
