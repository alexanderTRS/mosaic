using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mosaic.Modules.Media.Application.MediaAssets;
using Mosaic.Modules.Media.Domain.MediaAssets;

namespace Mosaic.Modules.Media.Infrastructure.Persistence;

public sealed class MediaAssetRepository : IMediaAssetRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly MediaDbContext dbContext;

    public MediaAssetRepository(MediaDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task Add(MediaAsset asset, CancellationToken cancellationToken)
    {
        await dbContext.Assets.AddAsync(ToRecord(asset), cancellationToken);
    }

    public async Task Update(MediaAsset asset, CancellationToken cancellationToken)
    {
        var record = await dbContext.Assets
            .SingleOrDefaultAsync(item => item.Id == asset.Id.Value, cancellationToken)
            ?? throw new MediaAssetNotFoundException(asset.Id.Value);

        Apply(record, asset);
    }

    public async Task<MediaAsset?> Get(MediaAssetId id, CancellationToken cancellationToken)
    {
        var record = await dbContext.Assets
            .AsNoTracking()
            .SingleOrDefaultAsync(asset => asset.Id == id.Value, cancellationToken);

        return record is null ? null : ToDomain(record);
    }

    public async Task<IReadOnlyList<MediaAsset>> List(int skip, int take, CancellationToken cancellationToken)
    {
        var records = await dbContext.Assets
            .AsNoTracking()
            .OrderByDescending(asset => asset.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return records.Select(ToDomain).ToArray();
    }

    private static MediaAssetRecord ToRecord(MediaAsset asset)
    {
        var record = new MediaAssetRecord { Id = asset.Id.Value };
        Apply(record, asset);
        return record;
    }

    private static void Apply(MediaAssetRecord record, MediaAsset asset)
    {
        record.FileName = asset.FileName;
        record.ContentType = asset.ContentType;
        record.Size = asset.Size;
        record.StoragePath = asset.StoragePath;
        record.PublicUrl = asset.PublicUrl;
        record.Width = asset.Width;
        record.Height = asset.Height;
        record.AltText = asset.Metadata.AltText;
        record.LocalizedAltText = JsonSerializer.Serialize(asset.Metadata.LocalizedAltText, JsonOptions);
        record.CreatedAt = asset.CreatedAt;
        record.CreatedBy = asset.CreatedBy;
    }

    private static MediaAsset ToDomain(MediaAssetRecord record)
    {
        var localizedAltText = JsonSerializer.Deserialize<Dictionary<string, string>>(
            record.LocalizedAltText,
            JsonOptions) ?? new Dictionary<string, string>();

        return MediaAsset.Restore(
            MediaAssetId.From(record.Id),
            record.FileName,
            record.ContentType,
            record.Size,
            record.StoragePath,
            record.PublicUrl,
            record.Width,
            record.Height,
            new MediaAssetMetadata(record.AltText, localizedAltText),
            record.CreatedAt,
            record.CreatedBy);
    }
}
