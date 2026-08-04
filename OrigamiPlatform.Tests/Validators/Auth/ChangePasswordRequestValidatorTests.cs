using Xunit;
using OrigamiPlatform.Application.Validators.Auth;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Validators.Auth;

public class ChangePasswordRequestValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_CurrentPasswordIsNullOrWhiteSpace_ThrowsDomainException(string? invalidPassword)
    {
        var exception = Assert.Throws<DomainException>(() => 
            ChangePasswordRequestValidator.Validate(invalidPassword!, "NewPass123", "NewPass123")
        );
        Assert.Equal("Current password is required.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NewPasswordIsNullOrWhiteSpace_ThrowsDomainException(string? invalidPassword)
    {
        var exception = Assert.Throws<DomainException>(() => 
            ChangePasswordRequestValidator.Validate("OldPass123", invalidPassword!, invalidPassword!)
        );
        Assert.Equal("New password is required.", exception.Message);
    }

    [Theory]
    [InlineData("short12")] // 7 chars
    [InlineData("toolongpasswordtoolongpasswordtoolongpasswordtoolong1")] // 51 chars
    public void Validate_NewPasswordLengthInvalid_ThrowsDomainException(string invalidPassword)
    {
        var exception = Assert.Throws<DomainException>(() => 
            ChangePasswordRequestValidator.Validate("OldPass123", invalidPassword, invalidPassword)
        );
        Assert.Equal("New password must be between 8 and 50 characters. BV-01.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ConfirmPasswordIsNullOrWhiteSpace_ThrowsDomainException(string? invalidConfirmPassword)
    {
        var exception = Assert.Throws<DomainException>(() => 
            ChangePasswordRequestValidator.Validate("OldPass123", "NewPass123", invalidConfirmPassword!)
        );
        Assert.Equal("Confirm password is required.", exception.Message);
    }

    [Fact]
    public void Validate_PasswordsDoNotMatch_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => 
            ChangePasswordRequestValidator.Validate("OldPass123", "NewPass123", "NewPass456")
        );
        Assert.Equal("Passwords do not match.", exception.Message);
    }

    [Fact]
    public void Validate_NewPasswordSameAsCurrent_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => 
            ChangePasswordRequestValidator.Validate("SamePass123", "SamePass123", "SamePass123")
        );
        Assert.Equal("New password must be different from the current password.", exception.Message);
    }

    [Fact]
    public void Validate_ValidInputs_DoesNotThrowException()
    {
        var exception = Record.Exception(() => 
            ChangePasswordRequestValidator.Validate("OldPass123", "NewPass123", "NewPass123")
        );
        Assert.Null(exception);
    }
}
