using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IChallengeStreakRepository
{
    /// <summary>Returns the user's ChallengeStreakLog, creating a new (zeroed) one if it doesn't exist yet.</summary>
    Task<ChallengeStreakLog> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task UpdateAsync(ChallengeStreakLog streak, CancellationToken ct = default);
    Task<List<ChallengeStreakLog>> GetTopAsync(int count, CancellationToken ct = default);
}
