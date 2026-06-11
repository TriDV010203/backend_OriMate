using OrigamiPlatform.Application.DTOs;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Commands.Auth;

public class ForgotPasswordHandler
{
    private readonly IUserRepository _users;
    private readonly IEmailService _email;

    public ForgotPasswordHandler(IUserRepository users, IEmailService email)
        => (_users, _email) = (users, email);

    public async Task<MessageResponse> HandleAsync(ForgotPasswordCommand cmd, CancellationToken ct = default)
    {
        // Generic response regardless of whether email exists — prevents enumeration
        const string genericResponse = "If that email is registered, a password reset link has been sent.";

        var user = await _users.GetByEmailAsync(cmd.Email.ToLowerInvariant(), ct);
        if (user is null)
            return new MessageResponse(genericResponse);

        var now = DateTime.UtcNow;
        user.PasswordResetToken = Guid.NewGuid().ToString("N");
        user.PasswordResetTokenExpiry = now.AddHours(1);
        user.UpdatedAt = now;

        await _users.UpdateAsync(user, ct);
        await _email.SendPasswordResetEmailAsync(user.Email, user.PasswordResetToken, ct);

        return new MessageResponse(genericResponse);
    }
}
