using OrigamiPlatform.Application.DTOs.TutorialProgress;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.TutorialProgress;

public class CompleteTutorialStepHandler
{
    private readonly ITutorialStepProgressRepository _progress;

    public CompleteTutorialStepHandler(ITutorialStepProgressRepository progress)
        => _progress = progress;

    public async Task<TutorialProgressDto> HandleAsync(
        CompleteTutorialStepCommand command, CancellationToken ct = default)
    {
        var step = await _progress.GetStepWithTutorialAsync(command.StepId, ct)
            ?? throw new NotFoundException("Tutorial step not found.");

        if (step.TutorialId != command.TutorialId)
            throw new NotFoundException("This step does not belong to the given tutorial.");

        if (step.Tutorial.Status != TutorialStatus.Published || step.Tutorial.IsDeleted)
            throw new DomainException("You can only track progress on a published tutorial.");

        // Each user can complete a step only once.
        if (await _progress.ExistsAsync(command.UserId, command.StepId, ct))
            throw new DomainException("You have already completed this step.");

        await _progress.AddAsync(new TutorialStepProgress
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            TutorialId = step.TutorialId,
            TutorialStepId = command.StepId,
            CompletedAt = DateTime.UtcNow
        }, ct);

        var total = await _progress.CountStepsAsync(command.TutorialId, ct);
        var completedIds = await _progress.GetCompletedStepIdsAsync(command.UserId, command.TutorialId, ct);
        return TutorialProgressFactory.Create(command.TutorialId, total, completedIds);
    }
}
