using Mosaic.Modules.Content.Application.Security;
using Mosaic.SharedKernel.Auditing;

namespace Mosaic.Modules.Content.Application.ContentTypes;

public sealed class DeprecateContentFieldHandler
{
    private readonly IContentTypeRepository repository;
    private readonly IContentUnitOfWork unitOfWork;
    private readonly IContentAccessService accessService;
    private readonly IAuditLog auditLog;

    public DeprecateContentFieldHandler(
        IContentTypeRepository repository,
        IContentUnitOfWork unitOfWork,
        IContentAccessService accessService,
        IAuditLog auditLog)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.accessService = accessService;
        this.auditLog = auditLog;
    }

    public async Task<ContentTypeDetails> Handle(
        DeprecateContentFieldCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var contentType = await repository.GetById(command.ContentTypeId, cancellationToken)
            ?? throw new ContentTypeNotFoundException(command.ContentTypeId);

        await accessService.EnsureCanManageContentType(contentType.ApiName.Value, cancellationToken);

        contentType.DeprecateField(command.FieldApiName);

        await repository.Update(contentType, cancellationToken);
        await auditLog.Record(
            AuditAction.ContentFieldDeprecated,
            $"{contentType.Id.Value}:{command.FieldApiName}",
            $"ContentTypeApiName={contentType.ApiName.Value};FieldApiName={command.FieldApiName}",
            cancellationToken);
        await unitOfWork.SaveChanges(cancellationToken);

        return contentType.ToDetails();
    }
}
