using Microsoft.EntityFrameworkCore;
using Mosaic.Modules.Content.Domain.ContentFields;
using Mosaic.Modules.Content.GraphQL.Dynamic;
using Mosaic.Modules.Content.Infrastructure.Persistence;

namespace Mosaic.Api.GraphQL;

public static class DynamicContentSchemaSnapshotLoader
{
    public static async Task<DynamicContentSchemaSnapshot> Load(
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Mosaic")
            ?? "Host=localhost;Port=5432;Database=mosaic;Username=mosaic;Password=mosaic";
        var options = new DbContextOptionsBuilder<ContentDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        try
        {
            await using var dbContext = new ContentDbContext(options);
            if (!await ContentSchemaExists(dbContext, cancellationToken))
            {
                return DynamicContentSchemaSnapshot.Empty;
            }

            var records = await dbContext.ContentTypes
                .AsNoTracking()
                .Include(contentType => contentType.Fields)
                .Where(contentType => contentType.Status == "Published")
                .OrderBy(contentType => contentType.ApiName)
                .ToListAsync(cancellationToken);

            var definitions = records
                .Select(record => new DynamicContentTypeDefinition(
                    record.Id,
                    record.ApiName,
                    record.ApiName,
                    Pluralize(record.ApiName),
                    ToGraphQlTypeName(record.ApiName),
                    record.SchemaVersion,
                    record.Fields
                        .OrderBy(field => field.ApiName)
                        .Select(field => new DynamicContentFieldDefinition(
                            field.ApiName,
                            Enum.Parse<FieldKind>(field.Kind),
                            Enum.Parse<LocalizationMode>(field.Localization),
                            field.IsRequired,
                            field.IsRepeatable,
                            field.IsDeprecated,
                            field.RelationTargetContentTypeApiName))
                        .ToArray()))
                .ToArray();

            return new DynamicContentSchemaSnapshot(definitions);
        }
        catch
        {
            return DynamicContentSchemaSnapshot.Empty;
        }
    }

    private static async Task<bool> ContentSchemaExists(
        ContentDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var tableCount = await dbContext.Database
            .SqlQueryRaw<int>(
                """
                SELECT COUNT(*)::int AS "Value"
                FROM information_schema.tables
                WHERE table_schema = 'content'
                    AND table_name IN ('content_types', 'content_fields')
                """)
            .SingleAsync(cancellationToken);

        return tableCount == 2;
    }

    private static string Pluralize(string apiName)
    {
        if (apiName.EndsWith("y", StringComparison.OrdinalIgnoreCase))
        {
            return $"{apiName[..^1]}ies";
        }

        if (apiName.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            return $"{apiName}List";
        }

        return $"{apiName}s";
    }

    private static string ToGraphQlTypeName(string apiName)
        => string.Concat(
            apiName[..1].ToUpperInvariant(),
            apiName.AsSpan(1));
}
