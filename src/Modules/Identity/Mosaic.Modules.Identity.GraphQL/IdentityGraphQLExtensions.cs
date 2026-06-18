using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mosaic.Modules.Identity.GraphQL.Login;

namespace Mosaic.Modules.Identity.GraphQL;

public static class IdentityGraphQLExtensions
{
    public static IRequestExecutorBuilder AddIdentityGraphQL(this IRequestExecutorBuilder builder)
        => builder.AddTypeExtension<IdentityMutations>();
}
