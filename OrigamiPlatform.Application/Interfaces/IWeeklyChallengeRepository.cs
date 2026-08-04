using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IWeeklyChallengeRepository
{
    Task<WeeklyChallenge?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(WeeklyChallenge challenge, CancellationToken ct = default);
}
