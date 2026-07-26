namespace OrigamiPlatform.Domain.Entities;

// FT-34: one photo submission per user per challenge (BR: unique on DailyChallengeId+UserId).
// Deliberately separate from Achievement — a challenge submission isn't tied to completing
// TutorialProgress and can repeat across different DailyChallenge rows for the same tutorial.
public class DailyChallengeSubmission
{
    public Guid Id { get; set; }
    public Guid DailyChallengeId { get; set; }
    public Guid UserId { get; set; }
    public string PhotoUrl { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    // Set once by CloseDailyChallengeResultHandler (null until the challenge closes).
    // Doubles as the source of truth for "how many times has this user finished Top 3" badges,
    // instead of a separate counter entity.
    public int? FinalRank { get; set; }

    public DailyChallenge DailyChallenge { get; set; } = null!;
    public User User { get; set; } = null!;
}
