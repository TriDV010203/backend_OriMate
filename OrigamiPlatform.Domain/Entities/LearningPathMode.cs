namespace OrigamiPlatform.Domain.Entities;

// A named tier ("Cơ bản", "Nâng cao", ...) that Learning Paths belong to, ordered by SortOrder.
// The mode with the lowest SortOrder is always open; later modes require completing a Learning
// Path in the immediately preceding mode plus an approved LearningPathModeUnlockTest submission.
public class LearningPathMode
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<LearningPath> LearningPaths { get; set; } = new List<LearningPath>();
    public LearningPathModeUnlockTest? UnlockTest { get; set; }
}
