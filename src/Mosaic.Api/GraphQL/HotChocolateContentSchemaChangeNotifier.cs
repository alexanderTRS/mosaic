using HotChocolate.Execution;
using Mosaic.Modules.Content.Application.ContentTypes;
using Mosaic.Modules.Content.GraphQL.Dynamic;

namespace Mosaic.Api.GraphQL;

public sealed class HotChocolateContentSchemaChangeNotifier : IContentSchemaChangeNotifier
{
    private readonly IConfiguration configuration;
    private readonly DynamicContentSchemaProvider schemaProvider;
    private readonly DynamicContentTypeModule typeModule;
    private readonly IRequestExecutorResolver executorResolver;
    private readonly ILogger<HotChocolateContentSchemaChangeNotifier> logger;

    public HotChocolateContentSchemaChangeNotifier(
        IConfiguration configuration,
        DynamicContentSchemaProvider schemaProvider,
        DynamicContentTypeModule typeModule,
        IRequestExecutorResolver executorResolver,
        ILogger<HotChocolateContentSchemaChangeNotifier> logger)
    {
        this.configuration = configuration;
        this.schemaProvider = schemaProvider;
        this.typeModule = typeModule;
        this.executorResolver = executorResolver;
        this.logger = logger;
    }

    public async Task PublishedContentTypesChanged(CancellationToken cancellationToken)
    {
        var snapshot = await DynamicContentSchemaSnapshotLoader.Load(configuration, cancellationToken);
        schemaProvider.Update(snapshot);
        typeModule.NotifyTypesChanged();
        executorResolver.EvictRequestExecutor(Schema.DefaultName);

        logger.LogInformation(
            "Rebuilt GraphQL schema for {ContentTypeCount} published content types",
            snapshot.ContentTypes.Count);
    }
}
