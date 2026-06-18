using Mosaic.Modules.Media.Application.Security;
using Mosaic.Modules.Media.Domain.MediaAssets;
using Mosaic.SharedKernel.Auditing;

namespace Mosaic.Modules.Media.Application.MediaAssets;

public sealed class UpdateMediaAssetMetadataHandler
{
    private readonly IMediaAssetRepository repository;
    private readonly IMediaUnitOfWork unitOfWork;
    private readonly IMediaAccessService accessService;
    private readonly IAuditLog auditLog;

    public UpdateMediaAssetMetadataHandler(
        IMediaAssetRepository repository,
        IMediaUnitOfWork unitOfWork,
        IMediaAccessService accessService,
        IAuditLog auditLog)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.accessService = accessService;
        this.auditLog = auditLog;
    }

    public async Task<MediaAssetDetails> Handle(UpdateMediaAssetMetadataCommand command, CancellationToken cancellationToken)
    {
        await accessService.EnsureCanManageMedia(cancellationToken);

        var asset = await repository.Get(MediaAssetId.From(command.Id), cancellationToken)
            ?? throw new MediaAssetNotFoundException(command.Id);

        asset.UpdateMetadata(new MediaAssetMetadata(command.AltText, command.LocalizedAltText));
        await repository.Update(asset, cancellationToken);
        await unitOfWork.SaveChanges(cancellationToken);
        await auditLog.Record(
            AuditAction.MediaAssetMetadataUpdated,
            asset.Id.Value.ToString(),
            $"FileName={asset.FileName}",
            cancellationToken);

        return MediaAssetMapper.ToDetails(asset);
    }
}
