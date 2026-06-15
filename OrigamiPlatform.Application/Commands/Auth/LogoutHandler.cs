using OrigamiPlatform.Application.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Auth;

public class LogoutHandler
{
    private readonly IUserRepository _users;

    public LogoutHandler(IUserRepository users) => _users = users;

    public async Task<MessageResponse> HandleAsync(LogoutCommand cmd, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException("User not found.");

        user.RefreshTokenHash = null;
        user.RefreshTokenExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _users.UpdateAsync(user, ct);

        return new MessageResponse("Logged out successfully.");
    }
}
