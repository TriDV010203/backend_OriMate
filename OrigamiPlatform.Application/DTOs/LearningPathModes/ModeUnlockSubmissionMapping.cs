using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.DTOs.LearningPathModes;

public static class ModeUnlockSubmissionMapping
{
    public static ModeUnlockSubmissionDto ToDto(this ModeUnlockSubmission submission)
        => new(
            submission.Id,
            submission.UserId,
            submission.User.Profile?.DisplayName ?? submission.User.Email,
            submission.User.Profile?.AvatarUrl,
            submission.LearningPathModeId,
            submission.LearningPathMode.Name,
            submission.TutorialId,
            submission.Tutorial.Title,
            submission.PhotoUrl,
            submission.Note,
            submission.Status.ToString(),
            submission.ReviewNote,
            submission.CreatedAt,
            submission.ReviewedAt);
}
