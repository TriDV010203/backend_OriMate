namespace OrigamiPlatform.Domain.Entities;

public class FamilyProjectStepProgress
{
    public Guid Id { get; set; }
    public Guid FamilyProjectId { get; set; }
    public Guid TutorialStepId { get; set; }
    public Guid CompletedByUserId { get; set; }
    public DateTime CompletedAt { get; set; }

    public FamilyProject FamilyProject { get; set; } = null!;
    public TutorialStep TutorialStep { get; set; } = null!;
    public User CompletedBy { get; set; } = null!;
}
