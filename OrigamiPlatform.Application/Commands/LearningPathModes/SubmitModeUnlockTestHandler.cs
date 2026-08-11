using OrigamiPlatform.Application.DTOs.LearningPathModes;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.LearningPathModes;

/// <summary>User submits a photo of the designated tutorial to unlock ModeId directly — the
/// "nhảy cóc" express lane for skipping straight to a higher mode without first completing a
/// Learning Path in the mode right before it (that's the OTHER, test-free way to unlock — see
/// GetLearningPathModesHandler). This is deliberately independent of Achievement — it never
/// reads, creates, or updates an Achievement row, so it doesn't matter whether the user already
/// marked TutorialId as completed there.</summary>
public class SubmitModeUnlockTestHandler
{
    private const int MaxPhotoUrlLength = 512;
    private const int MaxNoteLength = 1000;

    private readonly ILearningPathModeRepository _modes;
    private readonly ILearningPathModeUnlockTestRepository _unlockTests;
    private readonly IModeUnlockSubmissionRepository _submissions;
    private readonly ILearningPathCompletionRepository _pathCompletions;
    private readonly INotificationService _notifications;

    public SubmitModeUnlockTestHandler(
        ILearningPathModeRepository modes,
        ILearningPathModeUnlockTestRepository unlockTests,
        IModeUnlockSubmissionRepository submissions,
        ILearningPathCompletionRepository pathCompletions,
        INotificationService notifications)
        => (_modes, _unlockTests, _submissions, _pathCompletions, _notifications)
            = (modes, unlockTests, submissions, pathCompletions, notifications);

    public async Task<ModeUnlockSubmissionDto> HandleAsync(
        SubmitModeUnlockTestCommand command, CancellationToken ct = default)
    {
        var request = command.Request;

        var mode = await _modes.GetByIdAsync(command.ModeId, ct)
            ?? throw new NotFoundException($"Learning path mode {command.ModeId} not found.");
        if (!mode.IsActive)
            throw new DomainException("This mode is not currently available.");

        var unlockTest = await _unlockTests.GetByModeIdAsync(mode.Id, ct)
            ?? throw new DomainException("This mode has no unlock test configured yet.");

        var predecessor = await _modes.GetImmediatePredecessorAsync(mode.SortOrder, ct);
        if (predecessor is not null && await _pathCompletions.HasCompletedAnyInModeAsync(command.UserId, predecessor.Id, ct))
            throw new DomainException("You already unlocked this mode by completing a learning path in the previous mode — no test needed.");

        if (await _submissions.ExistsApprovedAsync(command.UserId, mode.Id, ct))
            throw new DomainException("You have already unlocked this mode.");

        var latest = await _submissions.GetLatestByUserAndModeAsync(command.UserId, mode.Id, ct);
        if (latest is { Status: ModeUnlockSubmissionStatus.Pending })
            throw new DomainException("Your previous submission for this test is still awaiting review.");

        if (string.IsNullOrWhiteSpace(request.PhotoUrl) || request.PhotoUrl.Length > MaxPhotoUrlLength)
            throw new DomainException($"PhotoUrl is required and must not exceed {MaxPhotoUrlLength} characters.");
        if (request.Note is { Length: > MaxNoteLength })
            throw new DomainException($"Note must not exceed {MaxNoteLength} characters.");

        var submission = new ModeUnlockSubmission
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            LearningPathModeId = mode.Id,
            TutorialId = unlockTest.TutorialId,
            PhotoUrl = request.PhotoUrl,
            Note = request.Note,
            Status = ModeUnlockSubmissionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _submissions.AddAsync(submission, ct);

        await NotifyReviewersAsync(mode.Name, submission.Id, ct);

        var created = await _submissions.GetByIdAsync(submission.Id, ct)
            ?? throw new NotFoundException("Submission not found after creation.");

        return created.ToDto();
    }

    private async Task NotifyReviewersAsync(string modeName, Guid submissionId, CancellationToken ct)
    {
        // Đã bỏ try-catch
        var message = $"Có bài nộp bài test mở khoá chế độ \"{modeName}\" đang chờ duyệt.";

        await _notifications.NotifyUsersWithRoleAsync(
            UserRoleType.Manager, NotificationType.NewModeUnlockSubmission, message, nameof(ModeUnlockSubmission), submissionId, ct);

        await _notifications.NotifyUsersWithRoleAsync(
            UserRoleType.Admin, NotificationType.NewModeUnlockSubmission, message, nameof(ModeUnlockSubmission), submissionId, ct);
    }
}
