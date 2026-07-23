using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IUserDailyQuestProgressRepository
{
    /// <summary>Returns today's progress row for the user/quest, creating a new (zeroed) one if it doesn't exist yet.</summary>
    Task<UserDailyQuestProgress> GetOrCreateAsync(
        Guid userId, Guid questId, DateOnly questDate, CancellationToken ct = default);

    Task<UserDailyQuestProgress?> GetAsync(
        Guid userId, Guid questId, DateOnly questDate, CancellationToken ct = default);

    Task UpdateAsync(UserDailyQuestProgress progress, CancellationToken ct = default);
}
