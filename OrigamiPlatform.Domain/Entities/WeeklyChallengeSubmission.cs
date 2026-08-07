namespace OrigamiPlatform.Domain.Entities;

// Một bài nộp ảnh mỗi user mỗi Thử thách tuần (BR: unique trên WeeklyChallengeId+UserId).
// Mirror DailyChallengeSubmission — FinalRank được CloseWeeklyChallengeResultHandler set khi đóng sổ.
public class WeeklyChallengeSubmission
{
    public Guid Id { get; set; }
    public Guid WeeklyChallengeId { get; set; }
    public Guid UserId { get; set; }
    public string PhotoUrl { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    public int? FinalRank { get; set; }

    public WeeklyChallenge WeeklyChallenge { get; set; } = null!;
    public User User { get; set; } = null!;
}
