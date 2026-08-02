namespace OrigamiPlatform.Domain.Entities;

// FT-28: records which PaperPattern a user has purchased — unique per (UserId, PaperPatternId).
public class UserPaperPattern
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PaperPatternId { get; set; }
    public DateTime PurchasedAt { get; set; }

    public User User { get; set; } = null!;
    public PaperPattern PaperPattern { get; set; } = null!;
}
