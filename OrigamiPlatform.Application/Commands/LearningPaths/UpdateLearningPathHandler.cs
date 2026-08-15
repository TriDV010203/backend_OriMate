using OrigamiPlatform.Application.DTOs.LearningPaths;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.LearningPaths;

/// <summary>Admin/Manager edits a learning path's content and fully replaces its item list —
/// allowed regardless of current status (Draft/Published/Archived), same admin authority
/// precedent as AdminUpdateTutorialHandler.</summary>
public class UpdateLearningPathHandler
{
    private readonly ILearningPathRepository _learningPathRepo;
    private readonly ITutorialRepository _tutorialRepo;
    private readonly ILearningPathModeRepository _modeRepo;

    public UpdateLearningPathHandler(
        ILearningPathRepository learningPathRepo, ITutorialRepository tutorialRepo,
        ILearningPathModeRepository modeRepo)
        => (_learningPathRepo, _tutorialRepo, _modeRepo) = (learningPathRepo, tutorialRepo, modeRepo);

    public async Task<LearningPathDto> HandleAsync(UpdateLearningPathCommand command, CancellationToken ct = default)
    {
        var request = command.Request;

        var learningPath = await _learningPathRepo.GetByIdForAdminAsync(command.LearningPathId, ct)
            ?? throw new NotFoundException($"Learning path {command.LearningPathId} not found.");

        if (request.Title.Length < 5 || request.Title.Length > 150)
            throw new DomainException("Title must be between 5 and 150 characters.");
        if (request.Description.Length < 20 || request.Description.Length > 1000)
            throw new DomainException("Description must be between 20 and 1000 characters.");

        var mode = await _modeRepo.GetByIdAsync(request.LearningPathModeId, ct)
            ?? throw new NotFoundException($"Learning path mode {request.LearningPathModeId} not found.");
        if (!mode.IsActive)
            throw new DomainException("This mode is not active.");

        var items = await LearningPathItemValidator.BuildItemsAsync(_tutorialRepo, request.TutorialIds, ct);

        learningPath.Title = request.Title;
        learningPath.Description = request.Description;
        learningPath.CoverImageUrl = request.CoverImageUrl;
        learningPath.LearningPathModeId = mode.Id;
        learningPath.UpdatedAt = DateTime.UtcNow;

        await _learningPathRepo.UpdateAsync(learningPath, ct);

        foreach (var item in items)
            item.LearningPathId = learningPath.Id;
        await _learningPathRepo.ReplaceItemsAsync(learningPath.Id, items, ct);

        var refreshed = await _learningPathRepo.GetByIdForAdminAsync(learningPath.Id, ct)
            ?? throw new NotFoundException($"Learning path {learningPath.Id} not found after update.");

        return refreshed.ToDto();
    }
}
