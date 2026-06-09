using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class Like
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public TargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
