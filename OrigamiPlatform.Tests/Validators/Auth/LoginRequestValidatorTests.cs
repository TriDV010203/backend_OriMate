using Xunit;
using OrigamiPlatform.Application.Validators.Auth;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Validators.Auth;

public class LoginRequestValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmailIsNullOrWhiteSpace_ThrowsDomainException(string invalidEmail)
    {
        // Arrange
        string validPassword = "password123";

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            LoginRequestValidator.Validate(invalidEmail, validPassword)
        );
        Assert.Equal("Email is required.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_PasswordIsNullOrWhiteSpace_ThrowsDomainException(string invalidPassword)
    {
        // Arrange
        string validEmail = "test@example.com";

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            LoginRequestValidator.Validate(validEmail, invalidPassword)
        );
        Assert.Equal("Password is required.", exception.Message);
    }

    [Fact]
    public void Validate_ValidInputs_DoesNotThrowException()
    {
        // Arrange
        string validEmail = "test@example.com";
        string validPassword = "password123";

        // Act
        var exception = Record.Exception(() =>
            LoginRequestValidator.Validate(validEmail, validPassword)
        );

        // Assert
        Assert.Null(exception);
    }
}
