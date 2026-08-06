using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface ILearningPathModeRepository
{
    Task<List<LearningPathMode>> GetAllAsync(bool includeInactive, CancellationToken ct = default);
    Task<LearningPathMode?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsBySortOrderAsync(int sortOrder, Guid? excludeId, CancellationToken ct = default);

    // Active mode with the largest SortOrder strictly below the given one — null if this is the entry mode.
    Task<LearningPathMode?> GetImmediatePredecessorAsync(int sortOrder, CancellationToken ct = default);

    Task<int> CountPathsAsync(Guid modeId, CancellationToken ct = default);

    Task AddAsync(LearningPathMode mode, CancellationToken ct = default);
    Task UpdateAsync(LearningPathMode mode, CancellationToken ct = default);
}
