using OrigamiPlatform.Application.DTOs.TutorialProgress;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.TutorialProgress;

public class RaiseStuckFlagHandler
{
    private readonly ITutorialRepository _tutorials;
    private readonly IStuckThreadRepository _stuckThreads;

    public RaiseStuckFlagHandler(ITutorialRepository tutorials, IStuckThreadRepository stuckThreads)
        => (_tutorials, _stuckThreads) = (tutorials, stuckThreads);

    public async Task<StuckThreadDto> HandleAsync(RaiseStuckFlagCommand command, CancellationToken ct = default)
    {
        var step = await _tutorials.GetStepWithTutorialAsync(command.StepId, ct)
            ?? throw new NotFoundException("Tutorial step not found.");

        if (step.TutorialId != command.TutorialId)
            throw new NotFoundException("This step does not belong to the given tutorial.");

        if (step.Tutorial.Status != TutorialStatus.Published || step.Tutorial.IsDeleted)
            throw new DomainException("You can only raise a stuck flag on a published tutorial.");

        // A user can only have one StuckThread per step — return the existing one instead of creating a duplicate.
        var existing = await _stuckThreads.GetByUserAndStepAsync(command.UserId, command.StepId, ct);
        if (existing != null)
            return ToDto(existing);

        var thread = new StuckThread
        {
            Id = Guid.NewGuid(),
            TutorialId = step.TutorialId,
            StepId = command.StepId,
            UserId = command.UserId,
            CreatedAt = DateTime.UtcNow
        };

        await _stuckThreads.AddAsync(thread, ct);

        return ToDto(thread);
    }

    private static StuckThreadDto ToDto(StuckThread thread)
        => new(thread.Id, thread.TutorialId, thread.StepId, thread.UserId, thread.CreatedAt);
}
