namespace OrigamiPlatform.Domain.Entities;

public class CreatorVipSettings
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public bool IsActive { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
