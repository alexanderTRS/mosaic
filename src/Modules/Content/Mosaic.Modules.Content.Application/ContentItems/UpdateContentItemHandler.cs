using Mosaic.SharedKernel.Auditing;

namespace Mosaic.Modules.Content.Application.ContentItems;

public sealed class UpdateContentItemHandler
{
    private readonly IContentItemRepository repository;
    private readonly IContentUnitOfWork unitOfWork;
    private readonly ContentItemMutationService mutationService;
    private readonly IAuditLog auditLog;

    public UpdateContentItemHandler(
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
        UpdateContentItemCommand command,
        CancellationToken cancellationToken)
    {
        var (item, contentType) = await mutationService.LoadExisting(command.ContentItemId, cancellationToken);
        await mutationService.EnsureCanManageSubmittedFields(contentType, command.Data, cancellationToken);

        await repository.AddVersion(item, cancellationToken);
        item.Update(contentType, command.Data, mutationService.UtcNow);
        await mutationService.EnsureUniqueFields(contentType, item, cancellationToken);
        await mutationService.EnsureRelationTargetsExist(contentType, item, cancellationToken);
        await repository.Update(item, cancellationToken);
        await auditLog.Record(
            AuditAction.ContentItemUpdated,
            item.Id.Value.ToString(),
            $"ContentTypeApiName={contentType.ApiName.Value}",
            cancellationToken);
        await unitOfWork.SaveChanges(cancellationToken);

        return ContentItemMapper.ToDetails(item, contentType.ApiName.Value);
    }
}
