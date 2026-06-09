namespace OrigamiPlatform.Domain.Entities;

public class Achievement
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TutorialId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Tutorial Tutorial { get; set; } = null!;
}
