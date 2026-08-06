using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

// A user's photo submission for a LearningPathModeUnlockTest. Deliberately separate from
// Achievement — submitting/approving here never creates, updates, or reads an Achievement row,
// regardless of whether the user already has one for TutorialId.
public class ModeUnlockSubmission
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid LearningPathModeId { get; set; }
    public Guid TutorialId { get; set; }
    public string PhotoUrl { get; set; } = string.Empty;
    public string? Note { get; set; }
    public ModeUnlockSubmissionStatus Status { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public User User { get; set; } = null!;
    public LearningPathMode LearningPathMode { get; set; } = null!;
    public Tutorial Tutorial { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}
