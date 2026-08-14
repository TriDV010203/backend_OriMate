using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Tutorials;

// BR-16, BR-TUT-01: Remove is the only terminal status
public class ManagerRemoveHandler
{
    private readonly ITutorialRepository _tutorialRepo;
    private readonly INotificationService _notifications;

    public ManagerRemoveHandler(ITutorialRepository tutorialRepo, INotificationService notifications)
        => (_tutorialRepo, _notifications) = (tutorialRepo, notifications);

    public async Task HandleAsync(ManagerRemoveCommand command, CancellationToken ct = default)
    {
        var reason = command.Request?.Reason;

        var tutorial = await _tutorialRepo.GetByIdWithStepsAsync(command.TutorialId, ct)
            ?? throw new NotFoundException($"Tutorial {command.TutorialId} not found.");

        if (tutorial.Status != TutorialStatus.Published)
            throw new DomainException("Only published tutorials can be removed.");

        var fromStatus = tutorial.Status;
        tutorial.Status = TutorialStatus.Removed;
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
            ToStatus = TutorialStatus.Removed,
            Action = "Remove",
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        }, ct);

        var removedMessage = string.IsNullOrWhiteSpace(reason)
            ? $"Hướng dẫn \"{tutorial.Title}\" của bạn đã bị quản lý gỡ bỏ."
            : $"Hướng dẫn \"{tutorial.Title}\" của bạn đã bị quản lý gỡ bỏ: {reason}";

        await _notifications.NotifyUserAsync(
            tutorial.AuthorId,
            NotificationType.TutorialRemoved,
            removedMessage,
            "Tutorial",
            tutorial.Id,
            ct);
    }
}
