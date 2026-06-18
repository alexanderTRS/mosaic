using Mosaic.SharedKernel.Security;

namespace Mosaic.Modules.Identity.Application.Management;

internal static class ManagementAuthorization
{
    public static void EnsureAdministrator(ICurrentUserAccessor currentUserAccessor)
    {
        var user = currentUserAccessor.CurrentUser;
        if (!user.IsAuthenticated || !user.IsAdministrator)
        {
            throw new AccessDeniedException("Only administrators can manage identity.");
        }
    }
}
