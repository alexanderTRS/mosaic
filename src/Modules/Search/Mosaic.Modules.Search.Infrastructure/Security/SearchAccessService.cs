using Mosaic.Modules.Search.Application.Security;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Modules.Search.Infrastructure.Security;

public sealed class SearchAccessService : ISearchAccessService
{
    private readonly ICurrentUserAccessor currentUserAccessor;

    public SearchAccessService(ICurrentUserAccessor currentUserAccessor)
    {
        this.currentUserAccessor = currentUserAccessor;
    }

    public Task EnsureCanManageSearch(CancellationToken cancellationToken)
    {
        var user = currentUserAccessor.CurrentUser;
        if (!user.IsAuthenticated || !user.IsAdministrator)
        {
            throw new AccessDeniedException("Only administrators can manage the search index.");
        }

        return Task.CompletedTask;
    }
}
