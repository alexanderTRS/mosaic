namespace Mosaic.Modules.Content.GraphQL.Dynamic;

public sealed class DynamicContentSchemaProvider
{
    private DynamicContentSchemaSnapshot snapshot;

    public DynamicContentSchemaProvider(DynamicContentSchemaSnapshot snapshot)
    {
        this.snapshot = snapshot;
    }

    public DynamicContentSchemaSnapshot Snapshot => snapshot;

    public void Update(DynamicContentSchemaSnapshot nextSnapshot)
    {
        snapshot = nextSnapshot;
    }
}
