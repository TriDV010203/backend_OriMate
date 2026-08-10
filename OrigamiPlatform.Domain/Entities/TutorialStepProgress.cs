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

    /// <summary>Soft-delete flag set by "uncomplete". Kept (never hard-deleted) so <see cref="HasBeenRewarded"/>
    /// survives an uncomplete→complete cycle and can't be farmed for repeat rewards.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>True once this row has ever triggered skill points/Hạt Gấp/streak/quest rewards.
    /// Set once, never reset — re-completing after an uncomplete does not re-fire rewards.</summary>
    public bool HasBeenRewarded { get; set; }

    public User User { get; set; } = null!;
    public Tutorial Tutorial { get; set; } = null!;
    public TutorialStep TutorialStep { get; set; } = null!;
}
