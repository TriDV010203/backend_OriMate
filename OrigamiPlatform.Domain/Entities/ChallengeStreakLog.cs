namespace OrigamiPlatform.Domain.Entities;

// FT-34: streak counter dedicated to Daily Challenge participation — deliberately separate
// from StreakLog (FT-26), which tracks general tutorial-step activity. Only submitting to
// today's DailyChallenge advances this streak.
public class ChallengeStreakLog
{
    public Guid UserId { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateOnly? LastSubmissionDate { get; set; }
    public int FreezeCount { get; set; }

    public User User { get; set; } = null!;
}
