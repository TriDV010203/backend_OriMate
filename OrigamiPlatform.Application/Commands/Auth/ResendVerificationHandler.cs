using OrigamiPlatform.Application.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Commands.Auth;

public class ResendVerificationHandler
{
    private readonly IUserRepository _users;
    private readonly IEmailService _email;

    public ResendVerificationHandler(IUserRepository users, IEmailService email)
        => (_users, _email) = (users, email);

    public async Task<MessageResponse> HandleAsync(ResendVerificationCommand cmd, CancellationToken ct = default)
    {
        var genericResponse = new MessageResponse("If that email is registered, a new verification link has been sent.");

        var user = await _users.GetByEmailAsync(cmd.Email.ToLowerInvariant(), ct);
        if (user is null)
            return genericResponse;

        if (user.Status == AccountStatus.Active)
            return new MessageResponse("Email already verified.");

        var now = DateTime.UtcNow;
        user.VerificationToken = Guid.NewGuid().ToString("N");
        user.TokenExpiry = now.AddHours(24);
        user.UpdatedAt = now;

        await _users.UpdateAsync(user, ct);
        await _email.SendVerificationEmailAsync(user.Email, user.VerificationToken, ct);

        return genericResponse;
    }
}
