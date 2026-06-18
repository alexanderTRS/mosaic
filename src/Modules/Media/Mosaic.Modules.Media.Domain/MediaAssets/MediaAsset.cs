using Mosaic.SharedKernel.Domain;

namespace Mosaic.Modules.Media.Domain.MediaAssets;

public sealed class MediaAsset : Entity<MediaAssetId>
{
    private MediaAsset(
        MediaAssetId id,
        string fileName,
        string contentType,
        long size,
        string storagePath,
        string? publicUrl,
        int? width,
        int? height,
        MediaAssetMetadata metadata,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        FileName = NormalizeRequired(fileName, nameof(fileName), 256);
        ContentType = NormalizeContentType(contentType);
        Size = EnsurePositive(size, nameof(size));
        StoragePath = NormalizeRequired(storagePath, nameof(storagePath), 1024);
        PublicUrl = NormalizeOptional(publicUrl, 2048);
        Width = EnsureDimension(width, nameof(width));
        Height = EnsureDimension(height, nameof(height));
        Metadata = metadata;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public string FileName { get; private set; }

    public string ContentType { get; private set; }

    public long Size { get; private set; }

    public string StoragePath { get; private set; }

    public string? PublicUrl { get; private set; }

    public int? Width { get; private set; }

    public int? Height { get; private set; }

    public MediaAssetMetadata Metadata { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public static MediaAsset Create(
        string fileName,
        string contentType,
        long size,
        string storagePath,
        string? publicUrl,
        int? width,
        int? height,
        MediaAssetMetadata metadata,
        DateTimeOffset createdAt,
        Guid? createdBy)
        => new(
            MediaAssetId.New(),
            fileName,
            contentType,
            size,
            storagePath,
            publicUrl,
            width,
            height,
            metadata,
            createdAt,
            createdBy);

    public static MediaAsset Restore(
        MediaAssetId id,
        string fileName,
        string contentType,
        long size,
        string storagePath,
        string? publicUrl,
        int? width,
        int? height,
        MediaAssetMetadata metadata,
        DateTimeOffset createdAt,
        Guid? createdBy)
        => new(id, fileName, contentType, size, storagePath, publicUrl, width, height, metadata, createdAt, createdBy);

    public void UpdateMetadata(MediaAssetMetadata metadata) => Metadata = metadata;

    private static string NormalizeContentType(string contentType)
    {
        var normalized = NormalizeRequired(contentType, nameof(contentType), 128).ToLowerInvariant();
        if (!normalized.Contains('/', StringComparison.Ordinal) || normalized.Any(char.IsWhiteSpace))
        {
            throw new DomainRuleViolationException("Content type must be a valid MIME type.");
        }

        return normalized;
    }

    private static string NormalizeRequired(string value, string name, int maxLength)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new DomainRuleViolationException($"{name} cannot be empty.");
        }

        if (normalized.Length > maxLength)
        {
            throw new DomainRuleViolationException($"{name} cannot be longer than {maxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainRuleViolationException($"Value cannot be longer than {maxLength} characters.");
        }

        return normalized;
    }

    private static long EnsurePositive(long value, string name)
    {
        if (value <= 0)
        {
            throw new DomainRuleViolationException($"{name} must be greater than zero.");
        }

        return value;
    }

    private static int? EnsureDimension(int? value, string name)
    {
        if (value <= 0)
        {
            throw new DomainRuleViolationException($"{name} must be greater than zero.");
        }

        return value;
    }
}
