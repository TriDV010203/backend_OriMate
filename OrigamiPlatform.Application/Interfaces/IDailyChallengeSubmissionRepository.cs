using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IDailyChallengeSubmissionRepository
{
    Task<bool> ExistsAsync(Guid dailyChallengeId, Guid userId, CancellationToken ct = default);
    Task AddAsync(DailyChallengeSubmission submission, CancellationToken ct = default);
    Task UpdateAsync(DailyChallengeSubmission submission, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<DailyChallengeSubmission> submissions, CancellationToken ct = default);

    /// <summary>All submissions for one challenge day, with User/Profile eager-loaded for display — bounded volume (1 per user per day), safe to load in full and rank/paginate in memory.</summary>
    Task<List<DailyChallengeSubmission>> GetByChallengeAsync(Guid dailyChallengeId, CancellationToken ct = default);

    /// <summary>Used for "reached Top N" cumulative badges (e.g. Top 3 five times).</summary>
    Task<int> CountByUserWithMaxRankAsync(Guid userId, int maxRank, CancellationToken ct = default);
}
