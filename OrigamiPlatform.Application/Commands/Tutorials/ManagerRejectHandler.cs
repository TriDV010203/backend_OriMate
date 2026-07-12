using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Tutorials;

// BR-TUT-01: Reject is not a terminal status — it returns the tutorial to the author for revision, only Remove is terminal
public class ManagerRejectHandler
{
    private readonly ITutorialRepository _tutorialRepo;
    private readonly INotificationService _notifications;

    public ManagerRejectHandler(ITutorialRepository tutorialRepo, INotificationService notifications)
        => (_tutorialRepo, _notifications) = (tutorialRepo, notifications);

    public async Task HandleAsync(ManagerRejectCommand command, CancellationToken ct = default)
    {
        var reason = command.Request.Reason;
        if (reason.Length < 10)
            throw new DomainException("Rejection reason must be at least 10 characters. BR-18.");

        var tutorial = await _tutorialRepo.GetByIdWithStepsAsync(command.TutorialId, ct)
            ?? throw new NotFoundException($"Tutorial {command.TutorialId} not found.");

        if (tutorial.Status != TutorialStatus.PendingManagerReview)
            throw new DomainException("Only tutorials pending manager review can be rejected.");

        var fromStatus = tutorial.Status;
        tutorial.Status = TutorialStatus.RevisionRequired;
        tutorial.UpdatedAt = DateTime.UtcNow;

        await _tutorialRepo.UpdateAsync(tutorial, ct);

        // IMMUTABLE — INSERT only (BR-17)
        await _tutorialRepo.AddReviewHistoryAsync(new TutorialReviewHistory
        {
            Id = Guid.NewGuid(),
            TutorialId = tutorial.Id,
            ReviewerId = command.ManagerId,
            ReviewerRole = UserRoleType.Manager,
            FromStatus = fromStatus,
            ToStatus = TutorialStatus.RevisionRequired,
            Action = "Reject",
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _notifications.NotifyUserAsync(
            tutorial.AuthorId,
            NotificationType.TutorialRejected,
            $"Your tutorial needs revision: {reason}",
            "Tutorial",
            tutorial.Id,
            ct);
    }
}
