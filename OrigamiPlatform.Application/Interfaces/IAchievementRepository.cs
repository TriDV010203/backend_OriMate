using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IAchievementRepository
{
    Task<Achievement?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Achievement?> GetByUserAndTutorialAsync(
        Guid userId,
        Guid tutorialId,
        CancellationToken ct = default);

    Task<(IEnumerable<Achievement> Items, int TotalCount)> GetByUserAsync(
        Guid userId,
        bool includePrivate,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<bool> PublishedTutorialExistsAsync(Guid tutorialId, CancellationToken ct = default);

    Task<int> CountByUserAsync(Guid userId, CancellationToken ct = default);

    Task<HashSet<Guid>> GetCompletedTutorialIdsAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(Achievement achievement, CancellationToken ct = default);

    Task UpdateAsync(Achievement achievement, CancellationToken ct = default);

    Task DeleteAsync(Achievement achievement, CancellationToken ct = default);
}
