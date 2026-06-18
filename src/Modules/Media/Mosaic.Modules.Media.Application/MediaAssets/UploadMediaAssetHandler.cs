using Microsoft.Extensions.Logging;
using Mosaic.Modules.Media.Application.Security;
using Mosaic.Modules.Media.Application.Storage;
using Mosaic.Modules.Media.Domain.MediaAssets;
using Mosaic.SharedKernel.Auditing;
using Mosaic.SharedKernel.Security;
using Mosaic.SharedKernel.Time;

namespace Mosaic.Modules.Media.Application.MediaAssets;

public sealed class UploadMediaAssetHandler
{
    private readonly IMediaAssetRepository repository;
    private readonly IMediaStorage storage;
    private readonly IMediaUnitOfWork unitOfWork;
    private readonly IMediaAccessService accessService;
    private readonly ICurrentUserAccessor currentUserAccessor;
    private readonly IClock clock;
    private readonly IAuditLog auditLog;
    private readonly ILogger<UploadMediaAssetHandler> logger;

    public UploadMediaAssetHandler(
        IMediaAssetRepository repository,
        IMediaStorage storage,
        IMediaUnitOfWork unitOfWork,
        IMediaAccessService accessService,
        ICurrentUserAccessor currentUserAccessor,
        IClock clock,
        IAuditLog auditLog,
        ILogger<UploadMediaAssetHandler> logger)
    {
        this.repository = repository;
        this.storage = storage;
        this.unitOfWork = unitOfWork;
        this.accessService = accessService;
        this.currentUserAccessor = currentUserAccessor;
        this.clock = clock;
        this.auditLog = auditLog;
        this.logger = logger;
    }

    public async Task<MediaAssetDetails> Handle(UploadMediaAssetCommand command, CancellationToken cancellationToken)
    {
        await accessService.EnsureCanManageMedia(cancellationToken);

        var id = MediaAssetId.New();
        var storedFile = await storage.Save(id, command.FileName, command.ContentType, command.Content, cancellationToken);
        var metadata = new MediaAssetMetadata(command.AltText, command.LocalizedAltText);
        var asset = MediaAsset.Restore(
            id,
            command.FileName,
            command.ContentType,
            command.Size,
            storedFile.StoragePath,
            storedFile.PublicUrl,
            command.Width,
            command.Height,
            metadata,
            clock.UtcNow,
            currentUserAccessor.CurrentUser.Id);

        await repository.Add(asset, cancellationToken);
        await unitOfWork.SaveChanges(cancellationToken);
        await auditLog.Record(
            AuditAction.MediaAssetUploaded,
            asset.Id.Value.ToString(),
            $"FileName={asset.FileName};ContentType={asset.ContentType};Size={asset.Size}",
            cancellationToken);

        logger.LogInformation("Uploaded media asset {MediaAssetId} with content type {ContentType}", asset.Id.Value, asset.ContentType);

        return MediaAssetMapper.ToDetails(asset);
    }
}
