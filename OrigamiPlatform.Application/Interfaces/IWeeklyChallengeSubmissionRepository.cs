using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IWeeklyChallengeSubmissionRepository
{
    Task<WeeklyChallengeSubmission?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<WeeklyChallengeSubmission>> GetByChallengeAsync(Guid challengeId, CancellationToken ct = default);
    Task UpdateAsync(WeeklyChallengeSubmission submission, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<WeeklyChallengeSubmission> submissions, CancellationToken ct = default);
}
