using Mosaic.SharedKernel.Auditing;

namespace Mosaic.Modules.Content.Application.ContentItems;

public sealed class UnpublishContentItemHandler
{
    private readonly IContentItemRepository repository;
    private readonly IContentUnitOfWork unitOfWork;
    private readonly ContentItemMutationService mutationService;
    private readonly IAuditLog auditLog;

    public UnpublishContentItemHandler(
        IContentItemRepository repository,
        IContentUnitOfWork unitOfWork,
        ContentItemMutationService mutationService,
        IAuditLog auditLog)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.mutationService = mutationService;
        this.auditLog = auditLog;
    }

    public async Task<ContentItemDetails> Handle(
        ChangeContentItemStatusCommand command,
        CancellationToken cancellationToken)
    {
        var (item, contentType) = await mutationService.LoadEditable(command.ContentItemId, cancellationToken);

        await repository.AddVersion(item, cancellationToken);
        item.Unpublish(mutationService.UtcNow);
        await repository.Update(item, cancellationToken);
        await auditLog.Record(
            AuditAction.ContentItemUnpublished,
            item.Id.Value.ToString(),
            $"ContentTypeApiName={contentType.ApiName.Value}",
            cancellationToken);
        await unitOfWork.SaveChanges(cancellationToken);

        return ContentItemMapper.ToDetails(item, contentType.ApiName.Value);
    }
}
