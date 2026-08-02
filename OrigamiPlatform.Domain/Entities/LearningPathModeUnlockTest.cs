namespace OrigamiPlatform.Domain.Entities;

// Admin-configured "test": the single Tutorial a user must fold and submit a photo of to unlock
// LearningPathModeId. Only one active config per target mode (upserted by Admin/Manager).
public class LearningPathModeUnlockTest
{
    public Guid Id { get; set; }
    public Guid LearningPathModeId { get; set; }
    public Guid TutorialId { get; set; }
    public string? Instructions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public LearningPathMode LearningPathMode { get; set; } = null!;
    public Tutorial Tutorial { get; set; } = null!;
}
