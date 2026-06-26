using OrigamiPlatform.Application.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Application.Validators.Auth;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Auth;

public class ChangePasswordHandler
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public ChangePasswordHandler(IUserRepository users, IPasswordHasher hasher)
        => (_users, _hasher) = (users, hasher);

    public async Task<MessageResponse> HandleAsync(ChangePasswordCommand cmd, CancellationToken ct = default)
    {
        ChangePasswordRequestValidator.Validate(cmd.CurrentPassword, cmd.NewPassword, cmd.ConfirmPassword);

        var user = await _users.GetByIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("User not found.");

        if (!_hasher.Verify(cmd.CurrentPassword, user.PasswordHash))
            throw new DomainException("Current password is incorrect.");

        var now = DateTime.UtcNow;
        user.PasswordHash = _hasher.Hash(cmd.NewPassword);
        // Invalidate all active sessions — user must log in again with new password
        user.RefreshTokenHash = null;
        user.RefreshTokenExpiresAt = null;
        user.UpdatedAt = now;

        await _users.UpdateAsync(user, ct);

        return new MessageResponse("Password changed successfully. Please log in again.");
    }
}
