using Microsoft.EntityFrameworkCore;
using Mosaic.Modules.Content.Application.ContentItems;
using Mosaic.Modules.Content.Domain.ContentItems;
using Mosaic.Modules.Content.Domain.ContentTypes;
using Npgsql;

namespace Mosaic.Modules.Content.Infrastructure.Persistence;

public sealed class ContentItemRepository : IContentItemRepository
{
    private readonly ContentDbContext dbContext;

    public ContentItemRepository(ContentDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<ContentItem?> GetDomainById(
        ContentItemId contentItemId,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.ContentItems
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == contentItemId.Value, cancellationToken);

        return record is null
            ? null
            : ContentItem.Restore(
                new ContentItemId(record.Id),
                new ContentTypeId(record.ContentTypeId),
                Enum.Parse<ContentItemStatus>(record.Status),
                record.Data,
                record.CreatedAt,
                record.UpdatedAt);
    }

    public async Task<ContentItemDetails?> GetById(
        ContentItemId contentItemId,
        CancellationToken cancellationToken)
    {
        var query =
            from item in dbContext.ContentItems.AsNoTracking()
            join type in dbContext.ContentTypes.AsNoTracking()
                on item.ContentTypeId equals type.Id
            where item.Id == contentItemId.Value
            select ToDetails(item, type.ApiName);

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ContentItemDetails>> List(
        string? contentTypeApiName,
        CancellationToken cancellationToken)
    {
        var query =
            from item in dbContext.ContentItems.AsNoTracking()
            join type in dbContext.ContentTypes.AsNoTracking()
                on item.ContentTypeId equals type.Id
            select new { Item = item, TypeApiName = type.ApiName };

        if (!string.IsNullOrWhiteSpace(contentTypeApiName))
        {
            query = query.Where(row => row.TypeApiName == contentTypeApiName);
        }

        var records = await query
            .OrderByDescending(row => row.Item.CreatedAt)
            .ToListAsync(cancellationToken);

        return records
            .Select(row => ToDetails(row.Item, row.TypeApiName))
            .ToArray();
    }

    public async Task<ContentItemsPage> Page(ListContentItemsQuery query, CancellationToken cancellationToken)
    {
        var rows =
            from item in dbContext.ContentItems.AsNoTracking()
            join type in dbContext.ContentTypes.AsNoTracking()
                on item.ContentTypeId equals type.Id
            select new { Item = item, TypeApiName = type.ApiName };

        if (!string.IsNullOrWhiteSpace(query.ContentTypeApiName))
        {
            rows = rows.Where(row => row.TypeApiName == query.ContentTypeApiName);
        }

        if (query.Status is not null)
        {
            var status = query.Status.ToString();
            rows = rows.Where(row => row.Item.Status == status);
        }

        var materializedRows = await rows.ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            materializedRows = materializedRows
                .Where(row => row.Item.Data.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var totalCount = materializedRows.Count;
        var orderedRows = (query.OrderBy?.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("createdat", false) => materializedRows.OrderBy(row => row.Item.CreatedAt),
            ("createdat", true) => materializedRows.OrderByDescending(row => row.Item.CreatedAt),
            ("updatedat", false) => materializedRows.OrderBy(row => row.Item.UpdatedAt),
            ("updatedat", true) => materializedRows.OrderByDescending(row => row.Item.UpdatedAt),
            ("status", false) => materializedRows.OrderBy(row => row.Item.Status),
            ("status", true) => materializedRows.OrderByDescending(row => row.Item.Status),
            _ => materializedRows.OrderByDescending(row => row.Item.CreatedAt)
        };

        var skip = Math.Max(0, query.Skip);
        var take = Math.Clamp(query.Take, 1, 100);
        var records = orderedRows
            .Skip(skip)
            .Take(take)
            .ToList();

        return new ContentItemsPage(
            records.Select(row => ToDetails(row.Item, row.TypeApiName)).ToArray(),
            totalCount,
            skip,
            take);
    }

    public async Task Add(ContentItem contentItem, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentItem);

        await dbContext.ContentItems.AddAsync(
            new ContentItemRecord
            {
                Id = contentItem.Id.Value,
                ContentTypeId = contentItem.ContentTypeId.Value,
                Status = contentItem.Status.ToString(),
                Data = contentItem.Data,
                CreatedAt = contentItem.CreatedAt,
                UpdatedAt = contentItem.UpdatedAt
            },
            cancellationToken);
    }

    public async Task Update(ContentItem contentItem, CancellationToken cancellationToken)
    {
        var existing = await dbContext.ContentItems.SingleAsync(
            item => item.Id == contentItem.Id.Value,
            cancellationToken);

        existing.Status = contentItem.Status.ToString();
        existing.Data = contentItem.Data;
        existing.UpdatedAt = contentItem.UpdatedAt;
    }

    public async Task AddVersion(ContentItem contentItem, CancellationToken cancellationToken)
    {
        var nextVersion = await dbContext.ContentItemVersions
            .Where(version => version.ContentItemId == contentItem.Id.Value)
            .Select(version => (int?)version.Version)
            .MaxAsync(cancellationToken) ?? 0;

        await dbContext.ContentItemVersions.AddAsync(
            new ContentItemVersionRecord
            {
                Id = Guid.NewGuid(),
                ContentItemId = contentItem.Id.Value,
                Version = nextVersion + 1,
                Status = contentItem.Status.ToString(),
                Data = contentItem.Data,
                CreatedAt = DateTimeOffset.UtcNow
            },
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<ContentItemVersionDetails>> ListVersions(
        ContentItemId contentItemId,
        CancellationToken cancellationToken)
    {
        var records = await dbContext.ContentItemVersions
            .AsNoTracking()
            .Where(version => version.ContentItemId == contentItemId.Value)
            .OrderByDescending(version => version.Version)
            .ToListAsync(cancellationToken);

        return records
            .Select(version => new ContentItemVersionDetails(
                version.Id,
                version.ContentItemId,
                version.Version,
                Enum.Parse<ContentItemStatus>(version.Status),
                version.Data,
                version.CreatedAt))
            .ToArray();
    }

    public async Task<bool> ExistsWithFieldValue(
        ContentTypeId contentTypeId,
        string fieldApiName,
        string fieldValueJson,
        ContentItemId? excludeContentItemId,
        CancellationToken cancellationToken)
    {
        var count = await dbContext.Database
            .SqlQueryRaw<int>(
                """
                SELECT COUNT(*)::int AS "Value"
                FROM content.content_items
                WHERE "ContentTypeId" = @contentTypeId
                    AND "Data" -> @fieldApiName = @fieldValue::jsonb
                    AND (@excludeContentItemId IS NULL OR "Id" <> @excludeContentItemId)
                """,
                new NpgsqlParameter("contentTypeId", contentTypeId.Value),
                new NpgsqlParameter("fieldApiName", fieldApiName),
                new NpgsqlParameter("fieldValue", fieldValueJson),
                new NpgsqlParameter("excludeContentItemId", excludeContentItemId?.Value ?? (object)DBNull.Value))
            .SingleAsync(cancellationToken);

        return count > 0;
    }

    public async Task<bool> ExistsById(
        ContentTypeId contentTypeId,
        ContentItemId contentItemId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ContentItems
            .AsNoTracking()
            .AnyAsync(item => item.Id == contentItemId.Value
                && item.ContentTypeId == contentTypeId.Value, cancellationToken);
    }

    private static ContentItemDetails ToDetails(ContentItemRecord item, string contentTypeApiName)
        => new(
            item.Id,
            item.ContentTypeId,
            contentTypeApiName,
            Enum.Parse<ContentItemStatus>(item.Status),
            item.Data,
            item.CreatedAt,
            item.UpdatedAt);
}
