using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class FamilyProjectMember
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public MemberStatus Status { get; set; }
    public DateTime InvitedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? JoinedAt { get; set; }

    public FamilyProject Project { get; set; } = null!;
    public User User { get; set; } = null!;
}
