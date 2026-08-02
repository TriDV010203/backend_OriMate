using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface ILearningPathCompletionRepository
{
    Task<bool> ExistsAsync(Guid userId, Guid learningPathId, CancellationToken ct = default);
    Task AddAsync(LearningPathCompletion completion, CancellationToken ct = default);
}
