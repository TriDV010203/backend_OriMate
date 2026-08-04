using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class WeeklyChallenge
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

    public Tutorial Tutorial { get; set; } = null!;
    public User? CreatedByUser { get; set; }
    public ICollection<WeeklyChallengeSubmission> Submissions { get; set; } = new List<WeeklyChallengeSubmission>();
}
