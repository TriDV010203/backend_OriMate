using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? VipSubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public TransactionStatus Status { get; set; }
    public string? ProofImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public VipSubscription? VipSubscription { get; set; }
}
