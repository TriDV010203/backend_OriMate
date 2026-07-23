namespace OrigamiPlatform.Domain.Entities;

public class StreakLog
{
    public Guid UserId { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateOnly? LastActiveDate { get; set; }
    public int FreezeCount { get; set; }

    public User User { get; set; } = null!;
}
