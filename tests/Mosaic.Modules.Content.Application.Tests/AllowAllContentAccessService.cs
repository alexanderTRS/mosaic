using Mosaic.Modules.Content.Application.Security;

namespace Mosaic.Modules.Content.Application.Tests;

internal sealed class AllowAllContentAccessService : IContentAccessService
{
    public Task EnsureCanCreateContentType(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task EnsureCanManageContentType(string contentTypeApiName, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task EnsureCanManageContentItems(string contentTypeApiName, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task EnsureCanManageContentFields(
        string contentTypeApiName,
        IReadOnlyCollection<ContentFieldAccessRequest> fields,
        CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task EnsureCanReadContentItems(string? contentTypeApiName, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
