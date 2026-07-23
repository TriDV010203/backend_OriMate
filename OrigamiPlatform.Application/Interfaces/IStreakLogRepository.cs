using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IStreakLogRepository
{
    /// <summary>Returns the user's StreakLog, creating a new (zeroed) one if it doesn't exist yet.</summary>
    Task<StreakLog> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task UpdateAsync(StreakLog streakLog, CancellationToken ct = default);
}
