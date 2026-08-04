using Xunit;
using OrigamiPlatform.Application.DTOs.Shop;
using OrigamiPlatform.Application.Validators.Shop;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Validators.Shop;

public class CreateShopLinkRequestValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_TitleIsNullOrWhiteSpace_ThrowsDomainException(string? invalidTitle)
    {
        var request = new CreateShopLinkRequest(invalidTitle!, "http://example.com", null, null);
        var exception = Assert.Throws<DomainException>(() => CreateShopLinkRequestValidator.Validate(request));
        Assert.Equal("Title is required.", exception.Message);
    }

    [Fact]
    public void Validate_TitleTooLong_ThrowsDomainException()
    {
        var longTitle = new string('a', 201);
        var request = new CreateShopLinkRequest(longTitle, "http://example.com", null, null);
        var exception = Assert.Throws<DomainException>(() => CreateShopLinkRequestValidator.Validate(request));
        Assert.Equal("Title must not exceed 200 characters.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_UrlIsNullOrWhiteSpace_ThrowsDomainException(string? invalidUrl)
    {
        var request = new CreateShopLinkRequest("Valid Title", invalidUrl!, null, null);
        var exception = Assert.Throws<DomainException>(() => CreateShopLinkRequestValidator.Validate(request));
        Assert.Equal("Url is required.", exception.Message);
    }

    [Fact]
    public void Validate_UrlTooLong_ThrowsDomainException()
    {
        var longUrl = new string('a', 501);
        var request = new CreateShopLinkRequest("Valid Title", longUrl, null, null);
        var exception = Assert.Throws<DomainException>(() => CreateShopLinkRequestValidator.Validate(request));
        Assert.Equal("Url must not exceed 500 characters.", exception.Message);
    }

    [Fact]
    public void Validate_CategoryTooLong_ThrowsDomainException()
    {
        var longCategory = new string('a', 101);
        var request = new CreateShopLinkRequest("Valid Title", "http://example.com", null, longCategory);
        var exception = Assert.Throws<DomainException>(() => CreateShopLinkRequestValidator.Validate(request));
        Assert.Equal("Category must not exceed 100 characters.", exception.Message);
    }

    [Fact]
    public void Validate_ValidInputs_DoesNotThrowException()
    {
        var request = new CreateShopLinkRequest("Valid Title", "http://example.com", null, "Category");
        var exception = Record.Exception(() => CreateShopLinkRequestValidator.Validate(request));
        Assert.Null(exception);
    }
}
