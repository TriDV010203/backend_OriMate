using OrigamiPlatform.Application.DTOs.LearningPaths;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.LearningPaths;

public class GetLearningPathByIdHandler
{
    private readonly ILearningPathRepository _learningPathRepo;

    public GetLearningPathByIdHandler(ILearningPathRepository learningPathRepo) => _learningPathRepo = learningPathRepo;

    public async Task<LearningPathDto> HandleAsync(GetLearningPathByIdQuery query, CancellationToken ct = default)
    {
        var learningPath = await _learningPathRepo.GetPublishedByIdAsync(query.Id, ct)
            ?? throw new NotFoundException($"Learning path {query.Id} not found.");

        return learningPath.ToDto();
    }
}
