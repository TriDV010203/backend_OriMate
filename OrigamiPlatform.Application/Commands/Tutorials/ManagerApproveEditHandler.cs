using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Tutorials;

public class ManagerApproveEditHandler
{
    private readonly ITutorialRepository _tutorialRepo;
    private readonly INotificationService _notifications;

    public ManagerApproveEditHandler(ITutorialRepository tutorialRepo, INotificationService notifications)
        => (_tutorialRepo, _notifications) = (tutorialRepo, notifications);

    public async Task HandleAsync(ManagerApproveEditCommand command, CancellationToken ct = default)
    {
        var workingCopy = await _tutorialRepo.GetByIdWithStepsAsync(command.WorkingCopyId, ct)
            ?? throw new NotFoundException($"Tutorial {command.WorkingCopyId} not found.");

        if (workingCopy.Status != TutorialStatus.PendingManagerReview)
            throw new DomainException("Only working copies pending manager review can be approved.");

        if (workingCopy.ParentTutorialId is null)
            throw new DomainException("This tutorial is not a working copy.");

        var originalId = workingCopy.ParentTutorialId.Value;

        // Snapshot step data before loading original — avoids cross-context tracking issues
        var editedSteps = workingCopy.Steps.OrderBy(s => s.StepOrder).ToList();

        var original = await _tutorialRepo.GetByIdWithStepsAsync(originalId, ct)
            ?? throw new NotFoundException($"Original tutorial {originalId} not found.");

        // Reconcile steps by position instead of delete-all-then-insert: updating existing rows
        // in place (rather than dropping and recreating every row) avoids unnecessary DB churn.
        var originalSteps = original.Steps.OrderBy(s => s.StepOrder).ToList();
        var reuseCount = Math.Min(originalSteps.Count, editedSteps.Count);

        for (var i = 0; i < reuseCount; i++)
        {
            originalSteps[i].StepOrder = editedSteps[i].StepOrder;
            originalSteps[i].Description = editedSteps[i].Description;
            originalSteps[i].ImageUrl = editedSteps[i].ImageUrl;
            originalSteps[i].UpdatedAt = DateTime.UtcNow;
        }

        var stepsToAdd = editedSteps.Skip(reuseCount).Select(s => new TutorialStep
        {
            Id = Guid.NewGuid(),
            TutorialId = originalId,
            StepOrder = s.StepOrder,
            Description = s.Description,
            ImageUrl = s.ImageUrl,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        var stepsToRemove = originalSteps.Skip(reuseCount).ToList();

        // Swap content fields (id, slug, publishedAt stay untouched)
        original.Title = workingCopy.Title;
        original.Description = workingCopy.Description;
        original.CategoryId = workingCopy.CategoryId;
        original.Difficulty = workingCopy.Difficulty;
        original.Type = workingCopy.Type;
        original.CoverImageUrl = workingCopy.CoverImageUrl;
        original.UpdatedAt = DateTime.UtcNow;

        // Flushes the scalar field changes above together with the reused-step updates set earlier
        await _tutorialRepo.UpdateAsync(original, ct);

        if (stepsToAdd.Count > 0)
            await _tutorialRepo.AddStepsAsync(stepsToAdd, ct);

        if (stepsToRemove.Count > 0)
            await _tutorialRepo.RemoveStepsAsync(stepsToRemove, ct);

        // IMMUTABLE — INSERT only (BR-17), recorded on the original tutorial
        await _tutorialRepo.AddReviewHistoryAsync(new TutorialReviewHistory
        {
            Id = Guid.NewGuid(),
            TutorialId = originalId,
            ReviewerId = command.ManagerId,
            ReviewerRole = UserRoleType.Manager,
            FromStatus = TutorialStatus.PendingManagerReview,
            ToStatus = TutorialStatus.Published,
            Action = "ApproveEdit",
            Reason = null,
            CreatedAt = DateTime.UtcNow
        }, ct);

        // Do not hard-delete the working copy — mark it Merged (terminal, never reused). BR: no hard-delete of content.
        workingCopy.Status = TutorialStatus.Merged;
        workingCopy.UpdatedAt = DateTime.UtcNow;
        await _tutorialRepo.UpdateAsync(workingCopy, ct);

        await _notifications.NotifyUserAsync(
            original.AuthorId,
            NotificationType.TutorialEditPublished,
            $"Bản chỉnh sửa hướng dẫn \"{original.Title}\" của bạn đã được duyệt và xuất bản.",
            "Tutorial",
            originalId,
            ct);
    }
}
