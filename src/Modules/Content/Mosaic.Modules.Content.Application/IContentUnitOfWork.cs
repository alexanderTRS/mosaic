namespace Mosaic.Modules.Content.Application;

public interface IContentUnitOfWork
{
    Task SaveChanges(CancellationToken cancellationToken);
}
