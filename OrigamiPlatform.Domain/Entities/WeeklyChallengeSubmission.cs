namespace OrigamiPlatform.Domain.Entities;

public class WeeklyChallengeSubmission
{
    public Guid Id { get; set; }
    public Guid WeeklyChallengeId { get; set; }
    public Guid UserId { get; set; }
    public string PhotoUrl { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    public int? FinalRank { get; set; }
    public int LikeCount { get; set; }

    public WeeklyChallenge WeeklyChallenge { get; set; } = null!;
    public User User { get; set; } = null!;
}
