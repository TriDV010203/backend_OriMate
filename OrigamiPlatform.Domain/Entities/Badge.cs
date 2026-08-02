using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

// FT-35: badge catalog, seeded — not admin-editable in v1. Threshold/DifficultyFilter meaning
// depends on Category (e.g. TutorialCount → Threshold = number of Achievements; ChallengeRank/
// Author → Threshold = number of times the milestone must repeat, null = first-time only).
public class Badge
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconEmoji { get; set; } = string.Empty;
    public BadgeCategory Category { get; set; }
    public int? Threshold { get; set; }
    public TutorialDifficulty? DifficultyFilter { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}
