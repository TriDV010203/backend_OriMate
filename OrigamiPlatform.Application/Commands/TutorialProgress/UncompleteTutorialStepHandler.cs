using OrigamiPlatform.Application.DTOs.TutorialProgress;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.TutorialProgress;

public class UncompleteTutorialStepHandler
{
    private readonly ITutorialStepProgressRepository _progress;

    public UncompleteTutorialStepHandler(ITutorialStepProgressRepository progress)
        => _progress = progress;

    public async Task<TutorialProgressDto> HandleAsync(
        UncompleteTutorialStepCommand command, CancellationToken ct = default)
    {
        var existing = await _progress.GetAsync(command.UserId, command.StepId, ct)
            ?? throw new NotFoundException("This step is not marked as completed.");

        await _progress.RemoveAsync(existing, ct);

        var total = await _progress.CountStepsAsync(command.TutorialId, ct);
        var completedIds = await _progress.GetCompletedStepIdsAsync(command.UserId, command.TutorialId, ct);
        return TutorialProgressFactory.Create(command.TutorialId, total, completedIds);
    }
}
