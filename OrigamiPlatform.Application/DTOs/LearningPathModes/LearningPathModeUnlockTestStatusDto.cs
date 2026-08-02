namespace OrigamiPlatform.Application.DTOs.LearningPathModes;

/// <summary>The unlock test for a mode, plus (when a user is known) their latest submission state:
/// "None" | "Pending" | "Approved" | "Rejected".</summary>
public record LearningPathModeUnlockTestStatusDto(
    Guid TutorialId,
    string TutorialTitle,
    string TutorialSlug,
    string? TutorialCoverImageUrl,
    string? Instructions,
    string MySubmissionStatus,
    string? MyReviewNote
);
