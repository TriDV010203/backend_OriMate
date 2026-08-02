using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class HatGapTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int Amount { get; set; }
    public HatGapTransactionType Type { get; set; }
    public string Source { get; set; } = string.Empty;
    public int BalanceAfter { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
