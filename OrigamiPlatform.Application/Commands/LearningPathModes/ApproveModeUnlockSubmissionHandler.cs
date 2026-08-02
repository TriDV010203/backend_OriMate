using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.LearningPathModes;

public class ApproveModeUnlockSubmissionHandler
{
    private readonly IModeUnlockSubmissionRepository _submissions;
    private readonly INotificationService _notifications;

    public ApproveModeUnlockSubmissionHandler(
        IModeUnlockSubmissionRepository submissions, INotificationService notifications)
        => (_submissions, _notifications) = (submissions, notifications);

    public async Task HandleAsync(ApproveModeUnlockSubmissionCommand command, CancellationToken ct = default)
    {
        var submission = await _submissions.GetByIdAsync(command.SubmissionId, ct)
            ?? throw new NotFoundException($"Submission {command.SubmissionId} not found.");

        if (submission.Status != ModeUnlockSubmissionStatus.Pending)
            throw new DomainException("Only a pending submission can be approved.");

        submission.Status = ModeUnlockSubmissionStatus.Approved;
        submission.ReviewedByUserId = command.ActorId;
        submission.ReviewedAt = DateTime.UtcNow;
        submission.ReviewNote = null;

        await _submissions.UpdateAsync(submission, ct);

        try
        {
            await _notifications.NotifyUserAsync(
                userId: submission.UserId,
                type: NotificationType.ModeUnlockApproved,
                message: $"Bài test của bạn đã được duyệt! Chế độ \"{submission.LearningPathMode.Name}\" đã được mở khoá 🎉",
                entityType: nameof(ModeUnlockSubmission),
                entityId: submission.Id,
                ct: ct
            );
        }
        catch
        {
            // notification failure must not affect the approval itself
        }
    }
}
