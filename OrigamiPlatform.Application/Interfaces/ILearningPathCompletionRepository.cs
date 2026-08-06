using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface ILearningPathCompletionRepository
{
    Task<bool> ExistsAsync(Guid userId, Guid learningPathId, CancellationToken ct = default);
    Task AddAsync(LearningPathCompletion completion, CancellationToken ct = default);

    // Has this user completed at least one Published LearningPath belonging to this mode?
    // Completing a path in mode N auto-unlocks mode N+1 with no test required.
    Task<bool> HasCompletedAnyInModeAsync(Guid userId, Guid learningPathModeId, CancellationToken ct = default);
}
