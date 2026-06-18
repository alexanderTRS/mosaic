using Microsoft.EntityFrameworkCore;

namespace Mosaic.Modules.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserRecord> Users => Set<UserRecord>();

    public DbSet<AccessTokenRecord> AccessTokens => Set<AccessTokenRecord>();

    public DbSet<ContentTypePermissionRecord> ContentTypePermissions => Set<ContentTypePermissionRecord>();

    public DbSet<ExternalIdentityRecord> ExternalIdentities => Set<ExternalIdentityRecord>();

    public DbSet<AuditEventRecord> AuditEvents => Set<AuditEventRecord>();

    public DbSet<RoleRecord> Roles => Set<RoleRecord>();

    public DbSet<GroupRecord> Groups => Set<GroupRecord>();

    public DbSet<UserRoleRecord> UserRoles => Set<UserRoleRecord>();

    public DbSet<UserGroupRecord> UserGroups => Set<UserGroupRecord>();

    public DbSet<GroupRoleRecord> GroupRoles => Set<GroupRoleRecord>();

    public DbSet<RoleContentTypePermissionRecord> RoleContentTypePermissions => Set<RoleContentTypePermissionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");

        modelBuilder.Entity<UserRecord>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(user => user.Id);
            builder.Property(user => user.UserName).HasMaxLength(128).IsRequired();
            builder.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
            builder.Property(user => user.CanViewGraphQLSchema).IsRequired();
            builder.Property(user => user.IsServiceAccount).HasDefaultValue(false).IsRequired();
            builder.HasIndex(user => user.UserName).IsUnique();
        });

        modelBuilder.Entity<AccessTokenRecord>(builder =>
        {
            builder.ToTable("access_tokens");
            builder.HasKey(token => token.Id);
            builder.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();
            builder.Property(token => token.Name).HasMaxLength(256);
            builder.Property(token => token.Kind).HasMaxLength(64).HasDefaultValue("UserLogin").IsRequired();
            builder.Property(token => token.ExpiresAt).IsRequired();
            builder.HasIndex(token => token.TokenHash).IsUnique();

            builder
                .HasOne<UserRecord>()
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContentTypePermissionRecord>(builder =>
        {
            builder.ToTable("content_type_permissions");
            builder.HasKey(permission => permission.Id);
            builder.Property(permission => permission.ContentTypeApiName).HasMaxLength(128).IsRequired();
            builder.Property(permission => permission.FieldApiName).HasMaxLength(128);
            builder.Property(permission => permission.Locale).HasMaxLength(32);
            builder.HasIndex(permission => new
            {
                permission.UserId,
                permission.ContentTypeApiName,
                permission.FieldApiName,
                permission.Locale
            }).IsUnique();

            builder
                .HasOne<UserRecord>()
                .WithMany()
                .HasForeignKey(permission => permission.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExternalIdentityRecord>(builder =>
        {
            builder.ToTable("external_identities");
            builder.HasKey(identity => identity.Id);
            builder.Property(identity => identity.Provider).HasMaxLength(64).IsRequired();
            builder.Property(identity => identity.Subject).HasMaxLength(256).IsRequired();
            builder.Property(identity => identity.Email).HasMaxLength(256);
            builder.Property(identity => identity.DisplayName).HasMaxLength(256);
            builder.HasIndex(identity => new { identity.Provider, identity.Subject }).IsUnique();
            builder.HasIndex(identity => identity.UserId);

            builder
                .HasOne<UserRecord>()
                .WithMany()
                .HasForeignKey(identity => identity.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditEventRecord>(builder =>
        {
            builder.ToTable("audit_events");
            builder.HasKey(auditEvent => auditEvent.Id);
            builder.Property(auditEvent => auditEvent.Action).HasMaxLength(128).IsRequired();
            builder.Property(auditEvent => auditEvent.Subject).HasMaxLength(256).IsRequired();
            builder.Property(auditEvent => auditEvent.ActorUserName).HasMaxLength(128);
            builder.Property(auditEvent => auditEvent.Details).HasMaxLength(2048);
            builder.HasIndex(auditEvent => auditEvent.OccurredAt);
            builder.HasIndex(auditEvent => auditEvent.Action);
        });

        modelBuilder.Entity<RoleRecord>(builder =>
        {
            builder.ToTable("roles");
            builder.HasKey(role => role.Id);
            builder.Property(role => role.Name).HasMaxLength(128).IsRequired();
            builder.Property(role => role.DisplayName).HasMaxLength(256).IsRequired();
            builder.Property(role => role.Preset).HasMaxLength(64).IsRequired();
            builder.HasIndex(role => role.Name).IsUnique();
        });

        modelBuilder.Entity<GroupRecord>(builder =>
        {
            builder.ToTable("groups");
            builder.HasKey(group => group.Id);
            builder.Property(group => group.Name).HasMaxLength(128).IsRequired();
            builder.Property(group => group.DisplayName).HasMaxLength(256).IsRequired();
            builder.HasIndex(group => group.Name).IsUnique();
        });

        modelBuilder.Entity<UserRoleRecord>(builder =>
        {
            builder.ToTable("user_roles");
            builder.HasKey(userRole => new { userRole.UserId, userRole.RoleId });
            builder
                .HasOne<UserRecord>()
                .WithMany()
                .HasForeignKey(userRole => userRole.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder
                .HasOne<RoleRecord>()
                .WithMany()
                .HasForeignKey(userRole => userRole.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserGroupRecord>(builder =>
        {
            builder.ToTable("user_groups");
            builder.HasKey(userGroup => new { userGroup.UserId, userGroup.GroupId });
            builder
                .HasOne<UserRecord>()
                .WithMany()
                .HasForeignKey(userGroup => userGroup.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder
                .HasOne<GroupRecord>()
                .WithMany()
                .HasForeignKey(userGroup => userGroup.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GroupRoleRecord>(builder =>
        {
            builder.ToTable("group_roles");
            builder.HasKey(groupRole => new { groupRole.GroupId, groupRole.RoleId });
            builder
                .HasOne<GroupRecord>()
                .WithMany()
                .HasForeignKey(groupRole => groupRole.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            builder
                .HasOne<RoleRecord>()
                .WithMany()
                .HasForeignKey(groupRole => groupRole.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoleContentTypePermissionRecord>(builder =>
        {
            builder.ToTable("role_content_type_permissions");
            builder.HasKey(permission => permission.Id);
            builder.Property(permission => permission.ContentTypeApiName).HasMaxLength(128).IsRequired();
            builder.Property(permission => permission.FieldApiName).HasMaxLength(128);
            builder.Property(permission => permission.Locale).HasMaxLength(32);
            builder.HasIndex(permission => new
            {
                permission.RoleId,
                permission.ContentTypeApiName,
                permission.FieldApiName,
                permission.Locale
            }).IsUnique();
            builder
                .HasOne<RoleRecord>()
                .WithMany()
                .HasForeignKey(permission => permission.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
