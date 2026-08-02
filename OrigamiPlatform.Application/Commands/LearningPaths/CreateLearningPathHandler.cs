using OrigamiPlatform.Application.DTOs.LearningPaths;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.LearningPaths;

/// <summary>Admin/Manager starts a new learning path as Draft, curating tutorials they've
/// already authored and published (Tutorial.IsOfficial). Publish is a separate step
/// (<see cref="PublishLearningPathHandler"/>) so a path can be assembled over time.</summary>
public class CreateLearningPathHandler
{
    private readonly ILearningPathRepository _learningPathRepo;
    private readonly ITutorialRepository _tutorialRepo;
    private readonly IBlockedWordService _blockedWords;

    public CreateLearningPathHandler(
        ILearningPathRepository learningPathRepo, ITutorialRepository tutorialRepo, IBlockedWordService blockedWords)
        => (_learningPathRepo, _tutorialRepo, _blockedWords) = (learningPathRepo, tutorialRepo, blockedWords);

    public async Task<LearningPathDto> HandleAsync(CreateLearningPathCommand command, CancellationToken ct = default)
    {
        var request = command.Request;

        if (request.Title.Length < 5 || request.Title.Length > 150)
            throw new DomainException("Title must be between 5 and 150 characters.");
        if (request.Description.Length < 20 || request.Description.Length > 1000)
            throw new DomainException("Description must be between 20 and 1000 characters.");

        if (await _blockedWords.ContainsBlockedWordAsync(request.Title, ct))
            throw new DomainException("Title contains a blocked word. BR-23.");
        if (await _blockedWords.ContainsBlockedWordAsync(request.Description, ct))
            throw new DomainException("Description contains a blocked word. BR-23.");

        var items = await LearningPathItemValidator.BuildItemsAsync(_tutorialRepo, request.TutorialIds, ct);

        var now = DateTime.UtcNow;
        var learningPath = new LearningPath
        {
            Id = Guid.NewGuid(),
            CreatedByUserId = command.ActorId,
            Title = request.Title,
            Description = request.Description,
            CoverImageUrl = request.CoverImageUrl,
            Status = LearningPathStatus.Draft,
            CreatedAt = now
        };

        foreach (var item in items)
        {
            item.LearningPathId = learningPath.Id;
            learningPath.Items.Add(item);
        }

        await _learningPathRepo.AddAsync(learningPath, ct);

        return learningPath.ToDto();
    }
}
