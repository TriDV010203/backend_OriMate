using OrigamiPlatform.Application.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Auth;

public class ResetPasswordHandler
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public ResetPasswordHandler(IUserRepository users, IPasswordHasher hasher)
        => (_users, _hasher) = (users, hasher);

    public async Task<MessageResponse> HandleAsync(ResetPasswordCommand cmd, CancellationToken ct = default)
    {
        var user = await _users.GetByPasswordResetTokenAsync(cmd.Token, ct)
            ?? throw new NotFoundException("Invalid or expired reset token.");

        if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
            throw new DomainException("Reset link has expired.");

        var now = DateTime.UtcNow;
        user.PasswordHash = _hasher.Hash(cmd.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = now;

        await _users.UpdateAsync(user, ct);

        return new MessageResponse("Password has been reset successfully.");
    }
}
