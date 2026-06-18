namespace Mosaic.Modules.Media.Application;

public interface IMediaUnitOfWork
{
    Task SaveChanges(CancellationToken cancellationToken);
}
