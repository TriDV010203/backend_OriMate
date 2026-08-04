using Xunit;
using OrigamiPlatform.Application.Validators.Auth;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Validators.Auth;

public class ForgotPasswordRequestValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmailIsNullOrWhiteSpace_ThrowsDomainException(string? invalidEmail)
    {
        var exception = Assert.Throws<DomainException>(() => 
            ForgotPasswordRequestValidator.Validate(invalidEmail!)
        );
        Assert.Equal("Email is required.", exception.Message);
    }

    [Theory]
    [InlineData("testexample.com")] // missing @
    [InlineData("test@examplecom")] // missing .
    public void Validate_InvalidEmailFormat_ThrowsDomainException(string invalidEmail)
    {
        var exception = Assert.Throws<DomainException>(() => 
            ForgotPasswordRequestValidator.Validate(invalidEmail)
        );
        Assert.Equal("A valid email address is required.", exception.Message);
    }

    [Fact]
    public void Validate_ValidEmail_DoesNotThrowException()
    {
        var exception = Record.Exception(() => 
            ForgotPasswordRequestValidator.Validate("test@example.com")
        );
        Assert.Null(exception);
    }
}
