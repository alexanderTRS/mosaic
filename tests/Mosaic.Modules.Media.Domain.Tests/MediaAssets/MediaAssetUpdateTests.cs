using FluentAssertions;
using Mosaic.Modules.Media.Domain.MediaAssets;
using Mosaic.SharedKernel.Domain;

namespace Mosaic.Modules.Media.Domain.Tests.MediaAssets;

public sealed class MediaAssetUpdateTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UpdateMetadata_should_replace_alt_text()
    {
        var asset = CreateAsset();
        asset.UpdateMetadata(new MediaAssetMetadata("New alt", new Dictionary<string, string>()));
        asset.Metadata.AltText.Should().Be("New alt");
    }

    [Fact]
    public void UpdateMetadata_should_replace_localized_alt_text()
    {
        var asset = CreateAsset();
        asset.UpdateMetadata(new MediaAssetMetadata(null, new Dictionary<string, string> { ["en"] = "English alt" }));
        asset.Metadata.LocalizedAltText.Should().ContainKey("en").WhoseValue.Should().Be("English alt");
    }

    [Fact]
    public void Create_should_store_width_and_height()
    {
        var asset = MediaAsset.Create("img.jpg", "image/jpeg", 1024, "path/img.jpg", null, 800, 600,
            MediaAssetMetadata.Empty, Now, null);
        asset.Width.Should().Be(800);
        asset.Height.Should().Be(600);
    }

    [Fact]
    public void Create_should_accept_null_width_and_height()
    {
        var act = () => MediaAsset.Create("img.jpg", "image/jpeg", 1024, "path/img.jpg", null, null, null,
            MediaAssetMetadata.Empty, Now, null);
        act.Should().NotThrow();
    }

    [Fact]
    public void Create_should_reject_zero_width()
    {
        var act = () => MediaAsset.Create("img.jpg", "image/jpeg", 1024, "path/img.jpg", null, 0, 600,
            MediaAssetMetadata.Empty, Now, null);
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_reject_zero_height()
    {
        var act = () => MediaAsset.Create("img.jpg", "image/jpeg", 1024, "path/img.jpg", null, 800, 0,
            MediaAssetMetadata.Empty, Now, null);
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_reject_empty_file_name()
    {
        var act = () => MediaAsset.Create("   ", "image/jpeg", 1024, "path/img.jpg", null, null, null,
            MediaAssetMetadata.Empty, Now, null);
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_reject_empty_storage_path()
    {
        var act = () => MediaAsset.Create("img.jpg", "image/jpeg", 1024, "   ", null, null, null,
            MediaAssetMetadata.Empty, Now, null);
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void Create_should_store_created_by()
    {
        var userId = Guid.NewGuid();
        var asset = MediaAsset.Create("img.jpg", "image/jpeg", 1024, "path/img.jpg", null, null, null,
            MediaAssetMetadata.Empty, Now, userId);
        asset.CreatedBy.Should().Be(userId);
    }

    [Fact]
    public void Create_should_accept_null_created_by()
    {
        var asset = MediaAsset.Create("img.jpg", "image/jpeg", 1024, "path/img.jpg", null, null, null,
            MediaAssetMetadata.Empty, Now, null);
        asset.CreatedBy.Should().BeNull();
    }

    [Fact]
    public void Create_should_store_public_url()
    {
        var asset = MediaAsset.Create("img.jpg", "image/jpeg", 1024, "path/img.jpg",
            "/media/assets/img.jpg", null, null, MediaAssetMetadata.Empty, Now, null);
        asset.PublicUrl.Should().Be("/media/assets/img.jpg");
    }

    [Fact]
    public void Create_should_normalize_content_type_to_lowercase()
    {
        var asset = MediaAsset.Create("img.jpg", "IMAGE/JPEG", 1024, "path/img.jpg", null, null, null,
            MediaAssetMetadata.Empty, Now, null);
        asset.ContentType.Should().Be("image/jpeg");
    }

    [Fact]
    public void Create_should_trim_file_name()
    {
        var asset = MediaAsset.Create("  img.jpg  ", "image/jpeg", 1024, "path/img.jpg", null, null, null,
            MediaAssetMetadata.Empty, Now, null);
        asset.FileName.Should().Be("img.jpg");
    }

    [Fact]
    public void MediaAssetMetadata_empty_should_have_null_alt_text_and_empty_localized()
    {
        MediaAssetMetadata.Empty.AltText.Should().BeNull();
        MediaAssetMetadata.Empty.LocalizedAltText.Should().BeEmpty();
    }

    [Fact]
    public void MediaAssetMetadata_should_reject_locale_with_space()
    {
        var act = () => new MediaAssetMetadata(null, new Dictionary<string, string> { ["ru ru"] = "text" });
        act.Should().Throw<DomainRuleViolationException>().WithMessage("*Locale*");
    }

    [Fact]
    public void MediaAssetMetadata_should_normalize_locale_to_lowercase()
    {
        var meta = new MediaAssetMetadata(null, new Dictionary<string, string> { ["RU-RU"] = "Текст" });
        meta.LocalizedAltText.Should().ContainKey("ru-ru");
    }

    private static MediaAsset CreateAsset() =>
        MediaAsset.Create("product.jpg", "image/jpeg", 2048, "ab/product.jpg",
            "/media/assets/product.jpg", 100, 100, new MediaAssetMetadata("Alt", new Dictionary<string, string>()),
            Now, null);
}
