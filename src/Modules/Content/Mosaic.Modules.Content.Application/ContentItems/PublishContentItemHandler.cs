using Mosaic.SharedKernel.Auditing;

namespace Mosaic.Modules.Content.Application.ContentItems;

public sealed class PublishContentItemHandler
{
    private readonly IContentItemRepository repository;
    private readonly IContentUnitOfWork unitOfWork;
    private readonly ContentItemMutationService mutationService;
    private readonly IAuditLog auditLog;

    public PublishContentItemHandler(
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
        item.Publish(mutationService.UtcNow);
        await repository.Update(item, cancellationToken);
        await auditLog.Record(
            AuditAction.ContentItemPublished,
            item.Id.Value.ToString(),
            $"ContentTypeApiName={contentType.ApiName.Value}",
            cancellationToken);
        await unitOfWork.SaveChanges(cancellationToken);

        return ContentItemMapper.ToDetails(item, contentType.ApiName.Value);
    }
}
