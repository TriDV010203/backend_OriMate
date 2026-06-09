using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class FamilySubscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TransactionId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Transaction Transaction { get; set; } = null!;
    public ICollection<FamilyProject> Projects { get; set; } = new List<FamilyProject>();
}
