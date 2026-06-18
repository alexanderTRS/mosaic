using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Mosaic.SharedKernel.Security;

namespace Mosaic.Modules.Identity.Infrastructure.Security;

public sealed class HttpCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public CurrentUser CurrentUser
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return new CurrentUser(
                    null,
                    null,
                    IsAuthenticated: false,
                    IsAdministrator: false,
                    CanViewGraphQLSchema: false);
            }

            var idClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = Guid.TryParse(idClaim, out var parsedId) ? parsedId : (Guid?)null;
            var isAdministrator = principal.FindFirstValue("is_administrator") == bool.TrueString;
            var canViewGraphQLSchema =
                isAdministrator
                || principal.FindFirstValue("can_view_graphql_schema") == bool.TrueString;

            return new CurrentUser(
                userId,
                principal.Identity.Name,
                IsAuthenticated: true,
                isAdministrator,
                canViewGraphQLSchema);
        }
    }
}
