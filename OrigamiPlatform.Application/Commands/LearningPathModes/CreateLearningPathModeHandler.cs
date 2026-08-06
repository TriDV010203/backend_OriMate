using OrigamiPlatform.Application.DTOs.LearningPathModes;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.LearningPathModes;

public class CreateLearningPathModeHandler
{
    private readonly ILearningPathModeRepository _modes;

    public CreateLearningPathModeHandler(ILearningPathModeRepository modes) => _modes = modes;

    public async Task<LearningPathModeAdminDto> HandleAsync(
        CreateLearningPathModeCommand command, CancellationToken ct = default)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
            throw new DomainException("Name must be between 1 and 100 characters.");
        if (request.Description is { Length: > 500 })
            throw new DomainException("Description must not exceed 500 characters.");
        if (request.SortOrder < 1)
            throw new DomainException("SortOrder must be a positive integer.");

        if (await _modes.ExistsBySortOrderAsync(request.SortOrder, excludeId: null, ct))
            throw new DomainException($"SortOrder {request.SortOrder} is already used by another mode.");

        var now = DateTime.UtcNow;
        var mode = new LearningPathMode
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            SortOrder = request.SortOrder,
            IsActive = true,
            CreatedAt = now
        };

        await _modes.AddAsync(mode, ct);

        return new LearningPathModeAdminDto(
            mode.Id, mode.Name, mode.Description, mode.SortOrder, mode.IsActive,
            PathCount: 0, UnlockTestTutorialId: null, UnlockTestTutorialTitle: null, UnlockTestInstructions: null,
            mode.CreatedAt, mode.UpdatedAt);
    }
}
