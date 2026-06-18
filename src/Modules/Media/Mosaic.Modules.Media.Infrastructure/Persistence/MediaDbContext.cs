using Microsoft.EntityFrameworkCore;
using Mosaic.Modules.Media.Application;

namespace Mosaic.Modules.Media.Infrastructure.Persistence;

public sealed class MediaDbContext : DbContext, IMediaUnitOfWork
{
    public MediaDbContext(DbContextOptions<MediaDbContext> options)
        : base(options)
    {
    }

    public DbSet<MediaAssetRecord> Assets => Set<MediaAssetRecord>();

    public Task SaveChanges(CancellationToken cancellationToken)
        => SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("media");

        modelBuilder.Entity<MediaAssetRecord>(builder =>
        {
            builder.ToTable("assets");
            builder.HasKey(asset => asset.Id);
            builder.Property(asset => asset.FileName).HasMaxLength(256).IsRequired();
            builder.Property(asset => asset.ContentType).HasMaxLength(128).IsRequired();
            builder.Property(asset => asset.StoragePath).HasMaxLength(1024).IsRequired();
            builder.Property(asset => asset.PublicUrl).HasMaxLength(2048);
            builder.Property(asset => asset.AltText).HasMaxLength(512);
            builder.Property(asset => asset.LocalizedAltText).HasColumnType("jsonb").IsRequired();
            builder.HasIndex(asset => asset.ContentType);
            builder.HasIndex(asset => asset.CreatedAt);
        });
    }
}
