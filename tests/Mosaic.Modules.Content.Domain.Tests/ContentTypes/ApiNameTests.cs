using FluentAssertions;
using Mosaic.Modules.Content.Domain.ContentTypes;
using Mosaic.SharedKernel.Domain;

namespace Mosaic.Modules.Content.Domain.Tests.ContentTypes;

public sealed class ApiNameTests
{
    [Theory]
    [InlineData("product")]
    [InlineData("productDemo")]
    [InlineData("product_demo")]
    [InlineData("product123")]
    [InlineData("p")]
    [InlineData("myContentType_v2")]
    public void From_should_accept_valid_api_names(string value)
    {
        var act = () => ApiName.From(value);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Product")]
    [InlineData("1product")]
    [InlineData("product-name")]
    [InlineData("product name")]
    [InlineData("product.name")]
    public void From_should_reject_invalid_api_names(string value)
    {
        var act = () => ApiName.From(value);
        act.Should().Throw<DomainRuleViolationException>();
    }

    [Fact]
    public void From_should_trim_whitespace()
    {
        var name = ApiName.From("  product  ");
        name.Value.Should().Be("product");
    }

    [Theory]
    [InlineData("product", "Product")]
    [InlineData("productDemo", "ProductDemo")]
    [InlineData("p", "P")]
    [InlineData("myType", "MyType")]
    public void GraphQlTypeName_should_capitalize_first_letter(string apiName, string expected)
    {
        var name = ApiName.From(apiName);
        name.GraphQlTypeName.Should().Be(expected);
    }

    [Fact]
    public void ToString_should_return_value()
    {
        var name = ApiName.From("product");
        name.ToString().Should().Be("product");
    }

    [Fact]
    public void Two_names_with_same_value_should_be_equal()
    {
        var a = ApiName.From("product");
        var b = ApiName.From("product");
        a.Should().Be(b);
    }

    [Fact]
    public void Two_names_with_different_values_should_not_be_equal()
    {
        var a = ApiName.From("product");
        var b = ApiName.From("category");
        a.Should().NotBe(b);
    }
}
