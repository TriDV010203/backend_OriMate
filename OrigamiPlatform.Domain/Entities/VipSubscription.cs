using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class VipSubscription
{
    public Guid Id { get; set; }
    public Guid SubscriberId { get; set; }
    public Guid CreatorId { get; set; }
    public Guid TransactionId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public User Subscriber { get; set; } = null!;
    public User Creator { get; set; } = null!;
    public Transaction Transaction { get; set; } = null!;
}
