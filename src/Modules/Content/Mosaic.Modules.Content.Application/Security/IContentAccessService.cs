namespace Mosaic.Modules.Content.Application.Security;

public interface IContentAccessService
{
    Task EnsureCanCreateContentType(CancellationToken cancellationToken);

    Task EnsureCanManageContentType(string contentTypeApiName, CancellationToken cancellationToken);

    Task EnsureCanManageContentItems(string contentTypeApiName, CancellationToken cancellationToken);

    Task EnsureCanManageContentFields(
        string contentTypeApiName,
        IReadOnlyCollection<ContentFieldAccessRequest> fields,
        CancellationToken cancellationToken);

    Task EnsureCanReadContentItems(string? contentTypeApiName, CancellationToken cancellationToken);
}
