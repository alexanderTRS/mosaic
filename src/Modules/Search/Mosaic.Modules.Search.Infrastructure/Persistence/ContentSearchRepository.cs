using Microsoft.EntityFrameworkCore;
using Mosaic.Modules.Search.Application.ContentSearch;
using Npgsql;

namespace Mosaic.Modules.Search.Infrastructure.Persistence;

public sealed class ContentSearchRepository : IContentSearchRepository
{
    private readonly SearchDbContext dbContext;

    public ContentSearchRepository(SearchDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<SearchContentItemsPage> Search(SearchContentItemsQuery query, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Database
            .SqlQueryRaw<SearchContentItemRow>(
                """
                WITH ranked AS (
                    SELECT
                        "ContentItemId" AS "Id",
                        "ContentTypeApiName",
                        "Status",
                        "Data",
                        "UpdatedAt",
                        ts_rank_cd(to_tsvector('simple', "SearchText"), plainto_tsquery('simple', @query))::double precision AS "Score"
                    FROM search.content_item_documents
                    WHERE (@contentTypeApiName IS NULL OR "ContentTypeApiName" = @contentTypeApiName)
                        AND to_tsvector('simple', "SearchText") @@ plainto_tsquery('simple', @query)
                )
                SELECT
                    "Id",
                    "ContentTypeApiName",
                    "Status",
                    "Data"::text AS "Data",
                    "UpdatedAt",
                    "Score",
                    COUNT(*) OVER()::int AS "TotalCount"
                FROM ranked
                ORDER BY "Score" DESC, "UpdatedAt" DESC
                OFFSET @skip
                LIMIT @take
                """,
                new NpgsqlParameter("query", query.Query),
                new NpgsqlParameter("contentTypeApiName", query.ContentTypeApiName ?? (object)DBNull.Value),
                new NpgsqlParameter("skip", query.Skip),
                new NpgsqlParameter("take", query.Take))
            .ToListAsync(cancellationToken);

        var totalCount = rows.FirstOrDefault()?.TotalCount ?? 0;
        var contentTypeFacets = await GetFacets(
            query,
            """
            SELECT "ContentTypeApiName" AS "Value", COUNT(*)::int AS "Count"
            FROM search.content_item_documents
            WHERE (@contentTypeApiName IS NULL OR "ContentTypeApiName" = @contentTypeApiName)
                AND to_tsvector('simple', "SearchText") @@ plainto_tsquery('simple', @query)
            GROUP BY "ContentTypeApiName"
            ORDER BY "Count" DESC, "ContentTypeApiName"
            """,
            cancellationToken);
        var statusFacets = await GetFacets(
            query,
            """
            SELECT "Status" AS "Value", COUNT(*)::int AS "Count"
            FROM search.content_item_documents
            WHERE (@contentTypeApiName IS NULL OR "ContentTypeApiName" = @contentTypeApiName)
                AND to_tsvector('simple', "SearchText") @@ plainto_tsquery('simple', @query)
            GROUP BY "Status"
            ORDER BY "Count" DESC, "Status"
            """,
            cancellationToken);

        return new SearchContentItemsPage(
            rows.Select(row => new SearchContentItemResult(
                row.Id,
                row.ContentTypeApiName,
                row.Status,
                row.Data,
                row.UpdatedAt,
                row.Score)).ToArray(),
            contentTypeFacets,
            statusFacets,
            totalCount,
            query.Skip,
            query.Take);
    }

    public async Task<int> Rebuild(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """TRUNCATE TABLE search.content_item_documents;""",
            cancellationToken);

        return await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO search.content_item_documents (
                "Id",
                "ContentItemId",
                "ContentTypeApiName",
                "Status",
                "Data",
                "SearchText",
                "UpdatedAt"
            )
            SELECT
                item."Id",
                item."Id",
                type."ApiName",
                item."Status",
                item."Data",
                item."Data"::text,
                item."UpdatedAt"
            FROM content.content_items item
            INNER JOIN content.content_types type ON type."Id" = item."ContentTypeId";
            """,
            cancellationToken);
    }

    private async Task<IReadOnlyCollection<SearchFacetValue>> GetFacets(
        SearchContentItemsQuery query,
        string sql,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.Database
            .SqlQueryRaw<SearchFacetRow>(
                sql,
                new NpgsqlParameter("query", query.Query),
                new NpgsqlParameter("contentTypeApiName", query.ContentTypeApiName ?? (object)DBNull.Value))
            .ToListAsync(cancellationToken);

        return rows.Select(row => new SearchFacetValue(row.Value, row.Count)).ToArray();
    }
}
