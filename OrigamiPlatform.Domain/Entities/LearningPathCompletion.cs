namespace OrigamiPlatform.Domain.Entities;

// Records that a user finished every tutorial in a Learning Path — awards LearningPathCompletionReward
// Hạt Gấp exactly once per (User, LearningPath).
public class LearningPathCompletion
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid LearningPathId { get; set; }
    public DateTime CompletedAt { get; set; }

    public User User { get; set; } = null!;
    public LearningPath LearningPath { get; set; } = null!;
}
