using HotChocolate;
using HotChocolate.Types;
using Mosaic.Modules.Identity.Application.AccessTokens;
using Mosaic.Modules.Identity.Application.Login;
using Mosaic.Modules.Identity.Application.Management;

namespace Mosaic.Modules.Identity.GraphQL.Login;

[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class IdentityMutations
{
    public Task<LoginResult> Login(
        LoginInput input,
        [Service] LoginHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(new LoginCommand(input.UserName, input.Password), cancellationToken);

    public Task<UserDetails> CreateUser(
        CreateUserInput input,
        [Service] CreateUserHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(
            new CreateUserCommand(
                input.UserName,
                input.Password,
                input.IsAdministrator,
                input.CanViewGraphQLSchema),
            cancellationToken);

    public Task<UserDetails> GrantContentTypeAccess(
        GrantContentTypeAccessInput input,
        [Service] GrantContentTypeAccessHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(
            new GrantContentTypeAccessCommand(
                input.UserId,
                input.ContentTypeApiName,
                input.CanManageSchema,
                input.CanManageItems,
                input.CanReadItems,
                input.FieldApiName,
                input.Locale),
            cancellationToken);

    public Task<RoleDetails> CreateRole(
        CreateRoleInput input,
        [Service] CreateRoleHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(
            new CreateRoleCommand(
                input.Name,
                input.DisplayName,
                input.Preset,
                input.CanCreateContentTypes,
                input.CanViewGraphQLSchema),
            cancellationToken);

    public Task<UserDetails> CreateServiceAccount(
        CreateServiceAccountInput input,
        [Service] CreateServiceAccountHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(
            new CreateServiceAccountCommand(
                input.Name,
                input.DisplayName,
                input.CanViewGraphQLSchema),
            cancellationToken);

    public Task<ServiceAccountTokenDetails> CreateServiceAccountToken(
        CreateServiceAccountTokenInput input,
        [Service] CreateServiceAccountTokenHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(
            new CreateServiceAccountTokenCommand(
                input.ServiceAccountId,
                input.Name,
                input.LifetimeDays),
            cancellationToken);

    public Task<GroupDetails> CreateGroup(
        CreateGroupInput input,
        [Service] CreateGroupHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(
            new CreateGroupCommand(input.Name, input.DisplayName),
            cancellationToken);

    public Task<UserDetails> AssignRoleToUser(
        AssignRoleToUserInput input,
        [Service] AssignRoleToUserHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(
            new AssignRoleToUserCommand(input.UserId, input.RoleId),
            cancellationToken);

    public Task<UserDetails> AssignUserToGroup(
        AssignUserToGroupInput input,
        [Service] AssignUserToGroupHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(
            new AssignUserToGroupCommand(input.UserId, input.GroupId),
            cancellationToken);

    public Task<GroupDetails> AssignRoleToGroup(
        AssignRoleToGroupInput input,
        [Service] AssignRoleToGroupHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(
            new AssignRoleToGroupCommand(input.GroupId, input.RoleId),
            cancellationToken);

    public Task<RoleDetails> GrantRoleContentTypeAccess(
        GrantRoleContentTypeAccessInput input,
        [Service] GrantRoleContentTypeAccessHandler handler,
        CancellationToken cancellationToken)
        => handler.Handle(
            new GrantRoleContentTypeAccessCommand(
                input.RoleId,
                input.ContentTypeApiName,
                input.CanManageSchema,
                input.CanManageItems,
                input.CanReadItems,
                input.FieldApiName,
                input.Locale),
            cancellationToken);

    public async Task<bool> RevokeAccessToken(
        RevokeAccessTokenInput input,
        [Service] RevokeAccessTokenHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(input.AccessToken, cancellationToken);
        return true;
    }
}

public sealed record LoginInput(string UserName, string Password);

public sealed record CreateUserInput(
    string UserName,
    string Password,
    bool IsAdministrator,
    bool CanViewGraphQLSchema);

public sealed record GrantContentTypeAccessInput(
    Guid UserId,
    string ContentTypeApiName,
    bool CanManageSchema,
    bool CanManageItems,
    bool CanReadItems,
    string? FieldApiName = null,
    string? Locale = null);

public sealed record CreateRoleInput(
    string Name,
    string DisplayName,
    PermissionPreset Preset,
    bool CanCreateContentTypes,
    bool CanViewGraphQLSchema);

public sealed record CreateServiceAccountInput(
    string Name,
    string DisplayName,
    bool CanViewGraphQLSchema);

public sealed record CreateServiceAccountTokenInput(
    Guid ServiceAccountId,
    string Name,
    int LifetimeDays);

public sealed record CreateGroupInput(string Name, string DisplayName);

public sealed record AssignRoleToUserInput(Guid UserId, Guid RoleId);

public sealed record AssignUserToGroupInput(Guid UserId, Guid GroupId);

public sealed record AssignRoleToGroupInput(Guid GroupId, Guid RoleId);

public sealed record GrantRoleContentTypeAccessInput(
    Guid RoleId,
    string ContentTypeApiName,
    bool CanManageSchema,
    bool CanManageItems,
    bool CanReadItems,
    string? FieldApiName = null,
    string? Locale = null);

public sealed record RevokeAccessTokenInput(string AccessToken);
