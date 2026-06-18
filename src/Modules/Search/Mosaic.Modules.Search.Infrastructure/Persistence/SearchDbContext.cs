using Microsoft.EntityFrameworkCore;

namespace Mosaic.Modules.Search.Infrastructure.Persistence;

public sealed class SearchDbContext : DbContext
{
    public SearchDbContext(DbContextOptions<SearchDbContext> options)
        : base(options)
    {
    }

    public DbSet<ContentSearchDocumentRecord> ContentItemDocuments => Set<ContentSearchDocumentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("search");

        modelBuilder.Entity<ContentSearchDocumentRecord>(builder =>
        {
            builder.ToTable("content_item_documents");
            builder.HasKey(document => document.Id);
            builder.Property(document => document.ContentTypeApiName).HasMaxLength(128).IsRequired();
            builder.Property(document => document.Status).HasMaxLength(32).IsRequired();
            builder.Property(document => document.Data).HasColumnType("jsonb").IsRequired();
            builder.Property(document => document.SearchText).IsRequired();
            builder.HasIndex(document => document.ContentItemId).IsUnique();
            builder.HasIndex(document => document.ContentTypeApiName);
            builder.HasIndex(document => document.Status);
        });
    }
}
