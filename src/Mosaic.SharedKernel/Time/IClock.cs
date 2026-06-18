namespace Mosaic.SharedKernel.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
