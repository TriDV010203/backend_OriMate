namespace OrigamiPlatform.Domain.Entities;

public class FamilyProjectStepProgress
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid StepId { get; set; }
    public Guid CompletedBy { get; set; }
    public DateTime CompletedAt { get; set; }

    public FamilyProject Project { get; set; } = null!;
    public TutorialStep Step { get; set; } = null!;
    public User CompletedByUser { get; set; } = null!;
}
