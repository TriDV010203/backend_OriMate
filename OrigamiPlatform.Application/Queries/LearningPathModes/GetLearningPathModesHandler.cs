using OrigamiPlatform.Application.DTOs.LearningPathModes;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Queries.LearningPathModes;

/// <summary>A non-entry mode unlocks for a user via EITHER of two independent routes:
/// (1) completing ≥1 Learning Path in the immediately preceding mode — no test needed, or
/// (2) an approved ModeUnlockSubmission for this mode — the "nhảy cóc" express lane for
/// skipping ahead without finishing the previous mode first.</summary>
public class GetLearningPathModesHandler
{
    private readonly ILearningPathModeRepository _modes;
    private readonly ILearningPathModeUnlockTestRepository _unlockTests;
    private readonly IModeUnlockSubmissionRepository _submissions;
    private readonly ILearningPathCompletionRepository _pathCompletions;

    public GetLearningPathModesHandler(
        ILearningPathModeRepository modes,
        ILearningPathModeUnlockTestRepository unlockTests,
        IModeUnlockSubmissionRepository submissions,
        ILearningPathCompletionRepository pathCompletions)
        => (_modes, _unlockTests, _submissions, _pathCompletions) = (modes, unlockTests, submissions, pathCompletions);

    public async Task<List<LearningPathModeDto>> HandleAsync(GetLearningPathModesQuery query, CancellationToken ct = default)
    {
        var modes = await _modes.GetAllAsync(includeInactive: false, ct);
        var result = new List<LearningPathModeDto>();

        for (var index = 0; index < modes.Count; index++)
        {
            var mode = modes[index];
            var isEntryMode = index == 0;

            if (isEntryMode)
            {
                result.Add(new LearningPathModeDto(
                    mode.Id, mode.Name, mode.Description, mode.SortOrder,
                    IsEntryMode: true, IsUnlocked: true, UnlockTest: null));
                continue;
            }

            var predecessor = modes[index - 1];
            var unlockTestEntity = await _unlockTests.GetByModeIdAsync(mode.Id, ct);

            var mySubmissionStatus = "None";
            string? myReviewNote = null;
            var completedPredecessorPath = false;
            if (query.CurrentUserId is Guid uid)
            {
                completedPredecessorPath = await _pathCompletions.HasCompletedAnyInModeAsync(uid, predecessor.Id, ct);

                var latest = await _submissions.GetLatestByUserAndModeAsync(uid, mode.Id, ct);
                if (latest is not null)
                {
                    mySubmissionStatus = latest.Status.ToString();
                    myReviewNote = latest.ReviewNote;
                }
            }

            var isUnlocked = completedPredecessorPath || mySubmissionStatus == nameof(ModeUnlockSubmissionStatus.Approved);

            var unlockTestDto = unlockTestEntity is null
                ? null
                : new LearningPathModeUnlockTestStatusDto(
                    unlockTestEntity.TutorialId,
                    unlockTestEntity.Tutorial.Title,
                    unlockTestEntity.Tutorial.Slug,
                    unlockTestEntity.Tutorial.CoverImageUrl,
                    unlockTestEntity.Instructions,
                    mySubmissionStatus,
                    myReviewNote);

            result.Add(new LearningPathModeDto(
                mode.Id, mode.Name, mode.Description, mode.SortOrder,
                IsEntryMode: false, isUnlocked, unlockTestDto));
        }

        return result;
    }
}
