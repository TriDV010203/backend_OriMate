using Xunit;
using OrigamiPlatform.Application.Validators.Auth;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Validators.Auth;

public class ResetPasswordRequestValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_TokenIsNullOrWhiteSpace_ThrowsDomainException(string? invalidToken)
    {
        var exception = Assert.Throws<DomainException>(() =>
            ResetPasswordRequestValidator.Validate(invalidToken!, "ValidPass123", "ValidPass123")
        );
        Assert.Equal("Token is required.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NewPasswordIsNullOrWhiteSpace_ThrowsDomainException(string? invalidPassword)
    {
        var exception = Assert.Throws<DomainException>(() =>
            ResetPasswordRequestValidator.Validate("valid-token", invalidPassword!, invalidPassword!)
        );
        Assert.Equal("New password is required.", exception.Message);
    }

    [Theory]
    [InlineData("short12")] // 7 chars
    [InlineData("toolongpasswordtoolongpasswordtoolongpasswordtoolong1")] // 51 chars
    public void Validate_NewPasswordLengthInvalid_ThrowsDomainException(string invalidPassword)
    {
        var exception = Assert.Throws<DomainException>(() =>
            ResetPasswordRequestValidator.Validate("valid-token", invalidPassword, invalidPassword)
        );
        Assert.Equal("Password must be between 8 and 50 characters. BV-01.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ConfirmPasswordIsNullOrWhiteSpace_ThrowsDomainException(string? invalidConfirmPassword)
    {
        var exception = Assert.Throws<DomainException>(() =>
            ResetPasswordRequestValidator.Validate("valid-token", "ValidPass123", invalidConfirmPassword!)
        );
        Assert.Equal("Confirm password is required.", exception.Message);
    }

    [Fact]
    public void Validate_PasswordsDoNotMatch_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() =>
            ResetPasswordRequestValidator.Validate("valid-token", "ValidPass123", "ValidPass456")
        );
        Assert.Equal("Passwords do not match.", exception.Message);
    }

    [Fact]
    public void Validate_ValidInputs_DoesNotThrowException()
    {
        var exception = Record.Exception(() =>
            ResetPasswordRequestValidator.Validate("valid-token", "ValidPass123", "ValidPass123")
        );
        Assert.Null(exception);
    }
}
