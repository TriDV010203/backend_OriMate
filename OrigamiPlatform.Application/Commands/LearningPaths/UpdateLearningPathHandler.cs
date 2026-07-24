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
    private readonly IBlockedWordService _blockedWords;

    public UpdateLearningPathHandler(
        ILearningPathRepository learningPathRepo, ITutorialRepository tutorialRepo, IBlockedWordService blockedWords)
        => (_learningPathRepo, _tutorialRepo, _blockedWords) = (learningPathRepo, tutorialRepo, blockedWords);

    public async Task<LearningPathDto> HandleAsync(UpdateLearningPathCommand command, CancellationToken ct = default)
    {
        var request = command.Request;

        var learningPath = await _learningPathRepo.GetByIdForAdminAsync(command.LearningPathId, ct)
            ?? throw new NotFoundException($"Learning path {command.LearningPathId} not found.");

        if (request.Title.Length < 5 || request.Title.Length > 150)
            throw new DomainException("Title must be between 5 and 150 characters.");
        if (request.Description.Length < 20 || request.Description.Length > 1000)
            throw new DomainException("Description must be between 20 and 1000 characters.");

        if (await _blockedWords.ContainsBlockedWordAsync(request.Title, ct))
            throw new DomainException("Title contains a blocked word. BR-23.");
        if (await _blockedWords.ContainsBlockedWordAsync(request.Description, ct))
            throw new DomainException("Description contains a blocked word. BR-23.");

        var items = await LearningPathItemValidator.BuildItemsAsync(_tutorialRepo, request.TutorialIds, ct);

        learningPath.Title = request.Title;
        learningPath.Description = request.Description;
        learningPath.CoverImageUrl = request.CoverImageUrl;
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
