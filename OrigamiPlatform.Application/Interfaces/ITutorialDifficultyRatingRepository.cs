using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Interfaces;

public interface ITutorialDifficultyRatingRepository
{
    Task<bool> ExistsAsync(Guid userId, Guid tutorialId, CancellationToken ct = default);

    /// <summary>No-ops (does not throw) on a concurrent duplicate — the unique index on
    /// (UserId, TutorialId) already guarantees only one row ever exists.</summary>
    Task AddAsync(TutorialDifficultyRating rating, CancellationToken ct = default);

    Task<Dictionary<PerceivedDifficulty, int>> GetCountsAsync(Guid tutorialId, CancellationToken ct = default);
}
