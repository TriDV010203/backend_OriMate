namespace OrigamiPlatform.Domain.Entities;

public class StuckThread
{
    public Guid Id { get; set; }
    public Guid TutorialId { get; set; }
    public Guid StepId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Tutorial Tutorial { get; set; } = null!;
    public TutorialStep Step { get; set; } = null!;
    public User User { get; set; } = null!;
}
