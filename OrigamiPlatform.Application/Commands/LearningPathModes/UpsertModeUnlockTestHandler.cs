using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.LearningPathModes;

/// <summary>Admin/Manager assigns (or replaces) the single Tutorial a user must fold and submit
/// a photo of to unlock ModeId. The entry mode (no active predecessor) can't have a test — it's
/// always open.</summary>
public class UpsertModeUnlockTestHandler
{
    private const int MaxInstructionsLength = 1000;

    private readonly ILearningPathModeRepository _modes;
    private readonly ILearningPathModeUnlockTestRepository _unlockTests;
    private readonly ITutorialRepository _tutorials;

    public UpsertModeUnlockTestHandler(
        ILearningPathModeRepository modes, ILearningPathModeUnlockTestRepository unlockTests, ITutorialRepository tutorials)
        => (_modes, _unlockTests, _tutorials) = (modes, unlockTests, tutorials);

    public async Task HandleAsync(UpsertModeUnlockTestCommand command, CancellationToken ct = default)
    {
        var request = command.Request;

        var mode = await _modes.GetByIdAsync(command.ModeId, ct)
            ?? throw new NotFoundException($"Learning path mode {command.ModeId} not found.");

        _ = await _modes.GetImmediatePredecessorAsync(mode.SortOrder, ct)
            ?? throw new DomainException("The entry mode (lowest SortOrder) is always open and cannot have an unlock test.");

        if (request.Instructions is { Length: > MaxInstructionsLength })
            throw new DomainException($"Instructions must not exceed {MaxInstructionsLength} characters.");

        var tutorials = await _tutorials.GetByIdsAsync(new[] { request.TutorialId }, ct);
        var tutorial = tutorials.FirstOrDefault()
            ?? throw new NotFoundException($"Tutorial {request.TutorialId} not found.");

        if (!tutorial.IsOfficial || tutorial.Status != TutorialStatus.Published || tutorial.IsDeleted)
            throw new DomainException("Only an official, published tutorial can be used as an unlock test.");

        await _unlockTests.UpsertAsync(mode.Id, tutorial.Id, request.Instructions, ct);
    }
}
