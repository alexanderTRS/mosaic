using Microsoft.Extensions.Options;
using Mosaic.Modules.Media.Application.Storage;
using Mosaic.Modules.Media.Domain.MediaAssets;

namespace Mosaic.Modules.Media.Infrastructure.Storage;

public sealed class LocalMediaStorage : IMediaStorage
{
    private readonly LocalMediaStorageOptions options;

    public LocalMediaStorage(IOptions<LocalMediaStorageOptions> options)
    {
        this.options = options.Value;
    }

    public async Task<StoredMediaFile> Save(
        MediaAssetId id,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName);
        var relativePath = Path.Combine(id.Value.ToString("N")[..2], $"{id.Value:N}{extension}");
        var absolutePath = ToAbsolutePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using var fileStream = File.Create(absolutePath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return new StoredMediaFile(relativePath.Replace('\\', '/'), $"/media/assets/{id.Value}/file");
    }

    public Task<MediaFile> Open(string storagePath, string contentType, CancellationToken cancellationToken)
    {
        var absolutePath = ToAbsolutePath(storagePath);
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException("Media file was not found in storage.", storagePath);
        }

        Stream stream = File.OpenRead(absolutePath);
        return Task.FromResult(new MediaFile(stream, contentType));
    }

    private string ToAbsolutePath(string relativePath)
    {
        var root = Path.GetFullPath(options.RootPath);
        var candidate = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!candidate.StartsWith(root, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Media storage path escapes configured root.");
        }

        return candidate;
    }
}
