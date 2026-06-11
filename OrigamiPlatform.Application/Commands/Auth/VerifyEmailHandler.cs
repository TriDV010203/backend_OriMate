using OrigamiPlatform.Application.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Auth;

public class VerifyEmailHandler
{
    private readonly IUserRepository _users;

    public VerifyEmailHandler(IUserRepository users) => _users = users;

    public async Task<MessageResponse> HandleAsync(VerifyEmailCommand cmd, CancellationToken ct = default)
    {
        var user = await _users.GetByVerificationTokenAsync(cmd.Token, ct)
            ?? throw new NotFoundException("Invalid verification token.");

        if (user.TokenExpiry < DateTime.UtcNow)
            throw new DomainException("Verification link has expired.");

        if (user.Status == AccountStatus.Active)
            return new MessageResponse("Email already verified.");

        user.Status = AccountStatus.Active;
        user.VerificationToken = null;
        user.TokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _users.UpdateAsync(user, ct);

        return new MessageResponse("Email verified successfully.");
    }
}
