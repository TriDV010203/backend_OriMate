namespace OrigamiPlatform.Domain.Entities;

// FT-35: earned badge record — unique per (UserId, BadgeId). ContextRefId optionally points at
// the entity that triggered the award (e.g. a DailyChallenge id for a ChallengeRank badge).
public class UserBadge
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid BadgeId { get; set; }
    public DateTime EarnedAt { get; set; }
    public Guid? ContextRefId { get; set; }

    public User User { get; set; } = null!;
    public Badge Badge { get; set; } = null!;
}
