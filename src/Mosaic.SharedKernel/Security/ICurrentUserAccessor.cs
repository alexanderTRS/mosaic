namespace Mosaic.SharedKernel.Security;

public interface ICurrentUserAccessor
{
    CurrentUser CurrentUser { get; }
}
