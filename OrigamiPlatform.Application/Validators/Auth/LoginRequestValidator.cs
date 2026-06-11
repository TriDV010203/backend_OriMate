using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Validators.Auth;

public static class LoginRequestValidator
{
    public static void Validate(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");

        if (string.IsNullOrWhiteSpace(password))
            throw new DomainException("Password is required.");
    }
}
