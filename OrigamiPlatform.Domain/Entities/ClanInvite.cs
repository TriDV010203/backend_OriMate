using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class ClanInvite
{
    public Guid Id { get; set; }
    public Guid ClanId { get; set; }
    public Guid UserId { get; set; }
    public ClanInviteStatus Status { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Clan Clan { get; set; } = null!;
    public User User { get; set; } = null!;
}
