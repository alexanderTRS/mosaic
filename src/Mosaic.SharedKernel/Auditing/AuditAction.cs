namespace Mosaic.SharedKernel.Auditing;

public static class AuditAction
{
    public const string LoginSucceeded = "identity.login.succeeded";
    public const string UserCreated = "identity.user.created";
    public const string ContentTypeAccessGranted = "identity.content_type_access.granted";
    public const string RoleCreated = "identity.role.created";
    public const string GroupCreated = "identity.group.created";
    public const string RoleAssignedToUser = "identity.role.assigned_to_user";
    public const string UserAssignedToGroup = "identity.user.assigned_to_group";
    public const string RoleAssignedToGroup = "identity.role.assigned_to_group";
    public const string RoleContentTypeAccessGranted = "identity.role_content_type_access.granted";
    public const string ServiceAccountCreated = "identity.service_account.created";
    public const string ServiceAccountTokenCreated = "identity.service_account_token.created";
    public const string ContentTypeCreated = "content.content_type.created";
    public const string ContentFieldAdded = "content.content_field.added";
    public const string ContentFieldDeprecated = "content.content_field.deprecated";
    public const string ContentTypePublished = "content.content_type.published";
    public const string ContentItemCreated = "content.content_item.created";
    public const string ContentItemUpdated = "content.content_item.updated";
    public const string ContentItemArchived = "content.content_item.archived";
    public const string ContentItemPublished = "content.content_item.published";
    public const string ContentItemUnpublished = "content.content_item.unpublished";
    public const string MediaAssetUploaded = "media.asset.uploaded";
    public const string MediaAssetMetadataUpdated = "media.asset.metadata_updated";
    public const string SearchContentReindexed = "search.content.reindexed";
}
