using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public class ManagerRejectEditHandler
{
    private readonly ITutorialRepository _tutorialRepo;
    private readonly INotificationService _notifications;

    public ManagerRejectEditHandler(ITutorialRepository tutorialRepo, INotificationService notifications)
        => (_tutorialRepo, _notifications) = (tutorialRepo, notifications);

    public async Task HandleAsync(ManagerRejectEditCommand command, CancellationToken ct = default)
    {
        var reason = command.Request.Reason;
        if (reason.Length < 10)
            throw new DomainException("Rejection reason must be at least 10 characters. BR-18.");

        var workingCopy = await _tutorialRepo.GetByIdWithStepsAsync(command.WorkingCopyId, ct)
            ?? throw new NotFoundException($"Tutorial {command.WorkingCopyId} not found.");

        if (workingCopy.Status != TutorialStatus.PendingManagerReview)
            throw new DomainException("Only working copies pending manager review can be rejected.");

        var fromStatus = workingCopy.Status;
        workingCopy.Status = TutorialStatus.RevisionRequired;
        workingCopy.UpdatedAt = DateTime.UtcNow;

        await _tutorialRepo.UpdateAsync(workingCopy, ct);

        // IMMUTABLE — INSERT only (BR-17)
        await _tutorialRepo.AddReviewHistoryAsync(new TutorialReviewHistory
        {
            Id = Guid.NewGuid(),
            TutorialId = workingCopy.Id,
            ReviewerId = command.ManagerId,
            ReviewerRole = UserRoleType.Manager,
            FromStatus = fromStatus,
            ToStatus = TutorialStatus.RevisionRequired,
            Action = "RejectEdit",
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _notifications.NotifyUserAsync(
            workingCopy.AuthorId,
            NotificationType.TutorialEditRejected,
            $"Bản chỉnh sửa hướng dẫn \"{workingCopy.Title}\" của bạn đã bị từ chối: {reason}",
            "Tutorial",
            workingCopy.Id,
            ct);
    }
}
