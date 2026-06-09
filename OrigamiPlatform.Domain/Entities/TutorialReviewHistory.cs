using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class TutorialReviewHistory
{
    public Guid Id { get; set; }
    public Guid TutorialId { get; set; }
    public Guid ReviewerId { get; set; }
    public UserRoleType ReviewerRole { get; set; }
    public TutorialStatus FromStatus { get; set; }
    public TutorialStatus ToStatus { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }

    public Tutorial Tutorial { get; set; } = null!;
    public User Reviewer { get; set; } = null!;
}
