using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Tutorials;

// BR-16: Manager-only final approval
public class ManagerPublishHandler
{
    private readonly ITutorialRepository _tutorialRepo;
    private readonly INotificationService _notifications;

    public ManagerPublishHandler(ITutorialRepository tutorialRepo, INotificationService notifications)
        => (_tutorialRepo, _notifications) = (tutorialRepo, notifications);

    public async Task HandleAsync(ManagerPublishCommand command, CancellationToken ct = default)
    {
        var tutorial = await _tutorialRepo.GetByIdWithStepsAsync(command.TutorialId, ct)
            ?? throw new NotFoundException($"Tutorial {command.TutorialId} not found.");

        if (tutorial.Status != TutorialStatus.PendingManagerReview)
            throw new DomainException("Only tutorials pending manager review can be published.");

        var fromStatus = tutorial.Status;
        tutorial.Status = TutorialStatus.Published;
        tutorial.PublishedAt = DateTime.UtcNow;
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
            ToStatus = TutorialStatus.Published,
            Action = "Publish",
            Reason = null,
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _notifications.NotifyUserAsync(
            tutorial.AuthorId,
            NotificationType.TutorialPublished,
            $"Chúc mừng! Hướng dẫn \"{tutorial.Title}\" của bạn đã được xuất bản.",
            "Tutorial",
            tutorial.Id,
            ct);
    }
}
