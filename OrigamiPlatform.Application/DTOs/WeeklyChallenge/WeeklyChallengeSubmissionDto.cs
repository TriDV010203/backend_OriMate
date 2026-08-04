namespace OrigamiPlatform.Application.DTOs.WeeklyChallenge;

public class WeeklyChallengeSubmissionDto
{
    public Guid Id { get; set; }
    public Guid WeeklyChallengeId { get; set; }
    public Guid UserId { get; set; }
    public string? UserDisplayName { get; set; }
    public string? UserAvatarUrl { get; set; }
    
    public string PhotoUrl { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public int? FinalRank { get; set; }
    
    public int? RelevanceScore { get; set; }
    
    public int LikeCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
}
