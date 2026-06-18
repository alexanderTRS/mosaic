using FluentAssertions;
using Mosaic.Modules.Media.Domain.MediaAssets;
using Mosaic.SharedKernel.Domain;

namespace Mosaic.Modules.Media.Domain.Tests.MediaAssets;

public sealed class MediaAssetTests
{
    [Fact]
    public void CreateShouldNormalizeMetadata()
    {
        var asset = MediaAsset.Create(
            " product.jpg ",
            "IMAGE/JPEG",
            15,
            "local/product.jpg",
            "/media/assets/file",
            100,
            200,
            new MediaAssetMetadata(
                " Main image ",
                new Dictionary<string, string> { ["RU-ru"] = " Главное фото " }),
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        asset.FileName.Should().Be("product.jpg");
        asset.ContentType.Should().Be("image/jpeg");
        asset.Metadata.AltText.Should().Be("Main image");
        asset.Metadata.LocalizedAltText.Should().ContainKey("ru-ru").WhoseValue.Should().Be("Главное фото");
    }

    [Fact]
    public void CreateShouldRejectInvalidSize()
    {
        var act = () => MediaAsset.Create(
            "product.jpg",
            "image/jpeg",
            0,
            "local/product.jpg",
            null,
            null,
            null,
            MediaAssetMetadata.Empty,
            DateTimeOffset.UtcNow,
            null);

        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void CreateShouldRejectInvalidContentType()
    {
        var act = () => MediaAsset.Create(
            "product.jpg",
            "image jpeg",
            10,
            "local/product.jpg",
            null,
            null,
            null,
            MediaAssetMetadata.Empty,
            DateTimeOffset.UtcNow,
            null);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("*MIME*");
    }

    [Fact]
    public void MetadataShouldRejectInvalidLocale()
    {
        var act = () => new MediaAssetMetadata(
            null,
            new Dictionary<string, string> { ["ru ru"] = "Главное фото" });

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("*Locale*");
    }
}
