namespace OrigamiPlatform.Domain.Entities;

public class FamilySubscription
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User Owner { get; set; } = null!;
}
