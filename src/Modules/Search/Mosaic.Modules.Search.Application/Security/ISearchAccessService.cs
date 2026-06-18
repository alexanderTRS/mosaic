namespace Mosaic.Modules.Search.Application.Security;

public interface ISearchAccessService
{
    Task EnsureCanManageSearch(CancellationToken cancellationToken);
}
