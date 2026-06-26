namespace OrigamiPlatform.Domain.Entities;

/// <summary>
/// Tracks an individual user's completion of a single tutorial step (solo learning progress),
/// independent of family projects.
/// </summary>
public class TutorialStepProgress
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TutorialId { get; set; }
    public Guid TutorialStepId { get; set; }
    public DateTime CompletedAt { get; set; }

    public User User { get; set; } = null!;
    public Tutorial Tutorial { get; set; } = null!;
    public TutorialStep TutorialStep { get; set; } = null!;
}
