using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.LearningPathModes;

public class RejectModeUnlockSubmissionHandler
{
    private const int MaxReasonLength = 500;

    private readonly IModeUnlockSubmissionRepository _submissions;
    private readonly INotificationService _notifications;

    public RejectModeUnlockSubmissionHandler(
        IModeUnlockSubmissionRepository submissions, INotificationService notifications)
        => (_submissions, _notifications) = (submissions, notifications);

    public async Task HandleAsync(RejectModeUnlockSubmissionCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Length > MaxReasonLength)
            throw new DomainException($"Reason is required and must not exceed {MaxReasonLength} characters.");

        var submission = await _submissions.GetByIdAsync(command.SubmissionId, ct)
            ?? throw new NotFoundException($"Submission {command.SubmissionId} not found.");

        if (submission.Status != ModeUnlockSubmissionStatus.Pending)
            throw new DomainException("Only a pending submission can be rejected.");

        submission.Status = ModeUnlockSubmissionStatus.Rejected;
        submission.ReviewedByUserId = command.ActorId;
        submission.ReviewedAt = DateTime.UtcNow;
        submission.ReviewNote = command.Reason;

        await _submissions.UpdateAsync(submission, ct);

        try
        {
            await _notifications.NotifyUserAsync(
                userId: submission.UserId,
                type: NotificationType.ModeUnlockRejected,
                message: $"Bài test mở khoá \"{submission.LearningPathMode.Name}\" của bạn chưa được duyệt: {command.Reason}",
                entityType: nameof(ModeUnlockSubmission),
                entityId: submission.Id,
                ct: ct
            );
        }
        catch
        {
            // notification failure must not affect the rejection itself
        }
    }
}
