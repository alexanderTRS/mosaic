namespace Mosaic.Modules.Content.Domain.ContentTypes;

public static class ReservedContentFieldNames
{
    private static readonly HashSet<string> Names = new(StringComparer.Ordinal)
    {
        "id",
        "createdAt",
        "updatedAt",
        "publishedAt",
        "contentType"
    };

    public static bool Contains(ApiName apiName)
    {
        ArgumentNullException.ThrowIfNull(apiName);

        return Names.Contains(apiName.Value);
    }
}
