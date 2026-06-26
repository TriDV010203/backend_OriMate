using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Users;

public class UpdateProfileHandler
{
    private readonly IUserRepository _users;

    public UpdateProfileHandler(IUserRepository users)
    {
        _users = users;
    }

    public async Task HandleAsync(UpdateProfileCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.DisplayName))
        {
            throw new DomainException("Tên hiển thị (DisplayName) không được để trống.");
        }

        if (!string.IsNullOrEmpty(cmd.Bio) && cmd.Bio.Length > 300)
        {
            throw new DomainException("Tiểu sử (Bio) không được vượt quá 300 ký tự.");
        }

        var user = await _users.GetByIdAsync(cmd.UserId, ct);
        if (user == null)
        {
            throw new NotFoundException("Không tìm thấy người dùng.");
        }

        if (user.Profile == null)
        {
            user.Profile = new UserProfile
            {
                UserId = user.Id,
                DisplayName = cmd.DisplayName,
                AvatarUrl = cmd.AvatarUrl,
                Bio = cmd.Bio,
                CreatedAt = DateTime.UtcNow
            };
        }
        else
        {
            user.Profile.DisplayName = cmd.DisplayName;
            user.Profile.AvatarUrl = cmd.AvatarUrl;
            user.Profile.Bio = cmd.Bio;
            user.Profile.UpdatedAt = DateTime.UtcNow;
        }

        await _users.UpdateAsync(user, ct);
    }
}