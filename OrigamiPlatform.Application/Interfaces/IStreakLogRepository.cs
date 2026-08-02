using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IStreakLogRepository
{
    /// <summary>Returns the user's StreakLog, creating a new (zeroed) one if it doesn't exist yet.</summary>
    Task<StreakLog> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task UpdateAsync(StreakLog streakLog, CancellationToken ct = default);

    // FT-30: users whose last active day was exactly `days` ago (GMT+7 calendar day), for the
    // re-engagement email trigger.
    Task<List<(Guid UserId, string Email)>> GetUsersInactiveForDaysAsync(int days, CancellationToken ct = default);
}
