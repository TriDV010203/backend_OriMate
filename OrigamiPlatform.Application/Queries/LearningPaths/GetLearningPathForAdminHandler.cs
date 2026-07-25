using OrigamiPlatform.Application.DTOs.LearningPaths;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.LearningPaths;

public class GetLearningPathForAdminHandler
{
    private readonly ILearningPathRepository _learningPathRepo;

    public GetLearningPathForAdminHandler(ILearningPathRepository learningPathRepo) => _learningPathRepo = learningPathRepo;

    public async Task<LearningPathDto> HandleAsync(GetLearningPathForAdminQuery query, CancellationToken ct = default)
    {
        var learningPath = await _learningPathRepo.GetByIdForAdminAsync(query.Id, ct)
            ?? throw new NotFoundException($"Learning path {query.Id} not found.");

        return learningPath.ToDto();
    }
}
