namespace Mosaic.Modules.Media.Application.Security;

public interface IMediaAccessService
{
    Task EnsureCanManageMedia(CancellationToken cancellationToken);

    Task EnsureCanReadMedia(CancellationToken cancellationToken);
}
