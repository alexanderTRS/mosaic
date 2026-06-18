using Microsoft.EntityFrameworkCore;
using Mosaic.Modules.Content.Application;

namespace Mosaic.Modules.Content.Infrastructure.Persistence;

public sealed class ContentDbContext : DbContext, IContentUnitOfWork
{
    public ContentDbContext(DbContextOptions<ContentDbContext> options)
        : base(options)
    {
    }

    public DbSet<ContentTypeRecord> ContentTypes => Set<ContentTypeRecord>();

    public DbSet<ContentFieldRecord> ContentFields => Set<ContentFieldRecord>();

    public DbSet<ContentItemRecord> ContentItems => Set<ContentItemRecord>();

    public DbSet<ContentItemVersionRecord> ContentItemVersions => Set<ContentItemVersionRecord>();

    public Task SaveChanges(CancellationToken cancellationToken)
        => SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("content");

        modelBuilder.Entity<ContentTypeRecord>(builder =>
        {
            builder.ToTable("content_types");
            builder.HasKey(contentType => contentType.Id);
            builder.Property(contentType => contentType.ApiName).HasMaxLength(128).IsRequired();
            builder.Property(contentType => contentType.DisplayName).HasMaxLength(256).IsRequired();
            builder.Property(contentType => contentType.Status).HasMaxLength(32).IsRequired();
            builder.Property(contentType => contentType.SchemaVersion).IsRequired();
            builder.HasIndex(contentType => contentType.ApiName).IsUnique();

            builder
                .HasMany(contentType => contentType.Fields)
                .WithOne()
                .HasForeignKey(field => field.ContentTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContentFieldRecord>(builder =>
        {
            builder.ToTable("content_fields");
            builder.HasKey(field => field.Id);
            builder.Property(field => field.ApiName).HasMaxLength(128).IsRequired();
            builder.Property(field => field.DisplayName).HasMaxLength(256).IsRequired();
            builder.Property(field => field.Kind).HasMaxLength(32).IsRequired();
            builder.Property(field => field.Localization).HasMaxLength(32).IsRequired();
            builder.Property(field => field.RegexPattern).HasMaxLength(512);
            builder.Property(field => field.RequiredLocales).HasColumnType("jsonb");
            builder.Property(field => field.DefaultValue).HasColumnType("jsonb");
            builder.Property(field => field.RelationTargetContentTypeApiName).HasMaxLength(128);
            builder.HasIndex(field => new { field.ContentTypeId, field.ApiName }).IsUnique();
        });

        modelBuilder.Entity<ContentItemRecord>(builder =>
        {
            builder.ToTable("content_items");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Status).HasMaxLength(32).IsRequired();
            builder.Property(item => item.Data).HasColumnType("jsonb").IsRequired();
            builder.HasIndex(item => item.ContentTypeId);

            builder
                .HasOne<ContentTypeRecord>()
                .WithMany()
                .HasForeignKey(item => item.ContentTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ContentItemVersionRecord>(builder =>
        {
            builder.ToTable("content_item_versions");
            builder.HasKey(version => version.Id);
            builder.Property(version => version.Status).HasMaxLength(32).IsRequired();
            builder.Property(version => version.Data).HasColumnType("jsonb").IsRequired();
            builder.HasIndex(version => new { version.ContentItemId, version.Version }).IsUnique();

            builder
                .HasOne<ContentItemRecord>()
                .WithMany()
                .HasForeignKey(version => version.ContentItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
