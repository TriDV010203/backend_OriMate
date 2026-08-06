using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IWeeklyChallengeSubmissionRepository
{
    Task<bool> ExistsAsync(Guid weeklyChallengeId, Guid userId, CancellationToken ct = default);
    Task AddAsync(WeeklyChallengeSubmission submission, CancellationToken ct = default);
    Task UpdateAsync(WeeklyChallengeSubmission submission, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<WeeklyChallengeSubmission> submissions, CancellationToken ct = default);

    /// <summary>All submissions for one weekly challenge, with User/Profile eager-loaded for display.</summary>
    Task<List<WeeklyChallengeSubmission>> GetByChallengeAsync(Guid weeklyChallengeId, CancellationToken ct = default);

    /// <summary>Used for "reached Top N" cumulative badges (e.g. Top 3 five times).</summary>
    Task<int> CountByUserWithMaxRankAsync(Guid userId, int maxRank, CancellationToken ct = default);
}
