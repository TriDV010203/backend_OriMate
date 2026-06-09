using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class UserRole
{
    public Guid UserId { get; set; }
    public UserRoleType Role { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
