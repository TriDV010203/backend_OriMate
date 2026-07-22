namespace OrigamiPlatform.Domain.Entities;

public class ClanMember
{
    public Guid Id { get; set; }
    public Guid ClanId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; }
    public int ContributionPoints { get; set; }

    public Clan Clan { get; set; } = null!;
    public User User { get; set; } = null!;
}
