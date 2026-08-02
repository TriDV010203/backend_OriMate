namespace OrigamiPlatform.Domain.Entities;

public class PersonalMilestone
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int Threshold { get; set; }
    public DateTime UnlockedAt { get; set; }

    public User User { get; set; } = null!;
}
