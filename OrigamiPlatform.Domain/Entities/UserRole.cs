using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class UserRole
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public UserRoleType RoleType { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
