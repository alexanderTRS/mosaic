using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.Modules.Content.Domain.ContentItems;
using Mosaic.Modules.Content.Domain.ContentTypes;
using Mosaic.Modules.Content.Infrastructure.Persistence;

namespace Mosaic.Infrastructure.Tests;

public sealed class ContentInfrastructureTests : IAsyncLifetime
{
    private InfrastructurePostgresDatabase database = null!;

    public async Task InitializeAsync()
    {
        database = new InfrastructurePostgresDatabase();
        await database.Initialize();
    }

    public async Task DisposeAsync()
    {
        await database.DisposeAsync();
    }

    [Fact]
    public async Task ContentTypeRepository_should_persist_and_restore_fields()
    {
        var contentType = ContentType.Create("productInfrastructure", "Product Infrastructure");
        contentType.AddField(ContentField.Create(
            "title",
            "Title",
            FieldKind.String,
            LocalizationMode.NonLocalized,
            isRequired: true));

        await using (var dbContext = database.CreateContentDbContext())
        {
            var repository = new ContentTypeRepository(dbContext);
            await repository.Add(contentType, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = database.CreateContentDbContext())
        {
            var repository = new ContentTypeRepository(dbContext);
            var restored = await repository.GetByApiName("productInfrastructure", CancellationToken.None);

            restored.Should().NotBeNull();
            restored!.Id.Should().Be(contentType.Id);
            restored.DisplayName.Should().Be("Product Infrastructure");
            restored.Fields.Should().ContainSingle(field =>
                field.ApiName.Value == "title"
                && field.Kind == FieldKind.String
                && field.Localization == LocalizationMode.NonLocalized
                && field.IsRequired);
        }
    }

    [Fact]
    public async Task ContentItemRepository_should_persist_jsonb_items_and_list_by_content_type()
    {
        var contentType = ContentType.Create("catalogItem", "Catalog Item");
        contentType.AddField(ContentField.Create(
            "title",
            "Title",
            FieldKind.String,
            LocalizationMode.NonLocalized,
            isRequired: true));
        contentType.Publish(new FixedClock(new DateTimeOffset(2026, 4, 26, 0, 0, 0, TimeSpan.Zero)));

        var contentItem = ContentItem.Create(
            contentType,
            """{"title":"Demo item"}""",
            new DateTimeOffset(2026, 4, 26, 1, 0, 0, TimeSpan.Zero));

        await using (var dbContext = database.CreateContentDbContext())
        {
            var contentTypeRepository = new ContentTypeRepository(dbContext);
            await contentTypeRepository.Add(contentType, CancellationToken.None);
            await dbContext.SaveChangesAsync();

            var contentItemRepository = new ContentItemRepository(dbContext);
            await contentItemRepository.Add(contentItem, CancellationToken.None);
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = database.CreateContentDbContext())
        {
            var jsonColumnType = await dbContext.Database
                .SqlQueryRaw<string>(
                    """
                    SELECT udt_name AS "Value"
                    FROM information_schema.columns
                    WHERE table_schema = 'content'
                        AND table_name = 'content_items'
                        AND column_name = 'Data'
                    """)
                .SingleAsync();

            jsonColumnType.Should().Be("jsonb");

            var repository = new ContentItemRepository(dbContext);
            var items = await repository.List("catalogItem", CancellationToken.None);

            items.Should().ContainSingle();
            var item = items.Single();
            item.Id.Should().Be(contentItem.Id.Value);
            item.ContentTypeApiName.Should().Be("catalogItem");

            using var data = JsonDocument.Parse(item.Data);
            data.RootElement.GetProperty("title").GetString().Should().Be("Demo item");
        }
    }

    private sealed class FixedClock : Mosaic.SharedKernel.Time.IClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}
