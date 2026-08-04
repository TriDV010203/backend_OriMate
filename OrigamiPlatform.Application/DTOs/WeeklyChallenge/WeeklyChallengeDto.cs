using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.WeeklyChallenge;

public class WeeklyChallengeDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Theme { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public Guid TutorialId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public WeeklyChallengeStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string TutorialTitle { get; set; } = string.Empty;
    public string TutorialSlug { get; set; } = string.Empty;
    public string TutorialDifficulty { get; set; } = string.Empty;
    public string? TutorialAuthorName { get; set; }

    public int SubmissionCount { get; set; }
    public bool HasSubmittedThisWeek { get; set; }
    public int MyWeeklyPoints { get; set; }
}
