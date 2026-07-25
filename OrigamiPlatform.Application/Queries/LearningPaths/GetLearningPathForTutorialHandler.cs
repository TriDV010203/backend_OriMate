using OrigamiPlatform.Application.DTOs.LearningPaths;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.LearningPaths;

public class GetLearningPathForTutorialHandler
{
    private readonly ILearningPathRepository _learningPathRepo;

    public GetLearningPathForTutorialHandler(ILearningPathRepository learningPathRepo) => _learningPathRepo = learningPathRepo;

    public async Task<LearningPathContextDto?> HandleAsync(
        GetLearningPathForTutorialQuery query, CancellationToken ct = default)
    {
        var path = await _learningPathRepo.GetPublishedPathContainingTutorialAsync(query.TutorialId, ct);
        if (path is null)
            return null;

        var orderedItems = path.Items.OrderBy(i => i.ItemOrder).ToList();
        var index = orderedItems.FindIndex(i => i.TutorialId == query.TutorialId);

        return new LearningPathContextDto(
            path.Id,
            path.Title,
            index,
            orderedItems.Count,
            index == orderedItems.Count - 1);
    }
}
