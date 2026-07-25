namespace OrigamiPlatform.Domain.Entities;

public class LearningPathItem
{
    public Guid Id { get; set; }
    public Guid LearningPathId { get; set; }
    public Guid TutorialId { get; set; }
    public int ItemOrder { get; set; }
    public DateTime CreatedAt { get; set; }

    public LearningPath LearningPath { get; set; } = null!;
    public Tutorial Tutorial { get; set; } = null!;
}
