using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

/// <summary>
/// A learner's perceived-difficulty rating for a tutorial, recorded once on first-time completion
/// (never updated — one row per user per tutorial, enforced by a unique index).
/// </summary>
public class TutorialDifficultyRating
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TutorialId { get; set; }
    public PerceivedDifficulty Rating { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Tutorial Tutorial { get; set; } = null!;
}
