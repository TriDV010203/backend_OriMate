namespace OrigamiPlatform.Domain.Entities;

public class FamilyProjectMember
{
    public Guid Id { get; set; }
    public Guid FamilyProjectId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; }

    public FamilyProject FamilyProject { get; set; } = null!;
    public User User { get; set; } = null!;
}
