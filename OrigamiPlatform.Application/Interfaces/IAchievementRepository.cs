using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

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

    /// <summary>Number of distinct completions (Achievement rows) recorded for a tutorial — shown publicly on the tutorial detail page.</summary>
    Task<int> CountByTutorialAsync(Guid tutorialId, CancellationToken ct = default);

    /// <summary>Used by FT-35 badge thresholds that only count completions of a given difficulty (e.g. "10 bài Khó").</summary>
    Task<int> CountByUserAndDifficultyAsync(Guid userId, TutorialDifficulty difficulty, CancellationToken ct = default);

    Task<HashSet<Guid>> GetCompletedTutorialIdsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>FT-31: distinct CategoryId of tutorials the user has an Achievement for — used to bias recommendations toward categories already learned.</summary>
    Task<List<int>> GetCompletedCategoryIdsAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(Achievement achievement, CancellationToken ct = default);

    Task UpdateAsync(Achievement achievement, CancellationToken ct = default);

    Task DeleteAsync(Achievement achievement, CancellationToken ct = default);
}
