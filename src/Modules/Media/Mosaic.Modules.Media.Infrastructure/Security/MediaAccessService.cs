using Mosaic.Modules.Media.Application.Security;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Modules.Media.Infrastructure.Security;

public sealed class MediaAccessService : IMediaAccessService
{
    private readonly ICurrentUserAccessor currentUserAccessor;

    public MediaAccessService(ICurrentUserAccessor currentUserAccessor)
    {
        this.currentUserAccessor = currentUserAccessor;
    }

    public Task EnsureCanManageMedia(CancellationToken cancellationToken)
    {
        var user = currentUserAccessor.CurrentUser;
        if (!user.IsAuthenticated || !user.IsAdministrator)
        {
            throw new AccessDeniedException("Only administrators can manage media assets.");
        }

        return Task.CompletedTask;
    }

    public Task EnsureCanReadMedia(CancellationToken cancellationToken)
    {
        if (!currentUserAccessor.CurrentUser.IsAuthenticated)
        {
            throw new AccessDeniedException("Authentication is required to read media assets.");
        }

        return Task.CompletedTask;
    }
}
