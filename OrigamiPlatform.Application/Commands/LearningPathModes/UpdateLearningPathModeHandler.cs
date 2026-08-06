using OrigamiPlatform.Application.DTOs.LearningPathModes;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.LearningPathModes;

public class UpdateLearningPathModeHandler
{
    private readonly ILearningPathModeRepository _modes;
    private readonly ILearningPathModeUnlockTestRepository _unlockTests;

    public UpdateLearningPathModeHandler(
        ILearningPathModeRepository modes, ILearningPathModeUnlockTestRepository unlockTests)
        => (_modes, _unlockTests) = (modes, unlockTests);

    public async Task<LearningPathModeAdminDto> HandleAsync(
        UpdateLearningPathModeCommand command, CancellationToken ct = default)
    {
        var request = command.Request;

        var mode = await _modes.GetByIdAsync(command.ModeId, ct)
            ?? throw new NotFoundException($"Learning path mode {command.ModeId} not found.");

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
            throw new DomainException("Name must be between 1 and 100 characters.");
        if (request.Description is { Length: > 500 })
            throw new DomainException("Description must not exceed 500 characters.");
        if (request.SortOrder < 1)
            throw new DomainException("SortOrder must be a positive integer.");

        if (await _modes.ExistsBySortOrderAsync(request.SortOrder, excludeId: mode.Id, ct))
            throw new DomainException($"SortOrder {request.SortOrder} is already used by another mode.");

        mode.Name = request.Name;
        mode.Description = request.Description;
        mode.SortOrder = request.SortOrder;
        mode.IsActive = request.IsActive;
        mode.UpdatedAt = DateTime.UtcNow;

        await _modes.UpdateAsync(mode, ct);

        var pathCount = await _modes.CountPathsAsync(mode.Id, ct);
        var unlockTest = await _unlockTests.GetByModeIdAsync(mode.Id, ct);

        return new LearningPathModeAdminDto(
            mode.Id, mode.Name, mode.Description, mode.SortOrder, mode.IsActive,
            pathCount, unlockTest?.TutorialId, unlockTest?.Tutorial.Title, unlockTest?.Instructions,
            mode.CreatedAt, mode.UpdatedAt);
    }
}
