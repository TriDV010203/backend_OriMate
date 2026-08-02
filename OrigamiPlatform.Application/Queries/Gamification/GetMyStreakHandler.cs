using OrigamiPlatform.Application.DTOs.Gamification;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Gamification;

public class GetMyStreakHandler
{
    private readonly IStreakLogRepository _streakLogs;

    public GetMyStreakHandler(IStreakLogRepository streakLogs) => _streakLogs = streakLogs;

    public async Task<StreakDto> HandleAsync(GetMyStreakQuery query, CancellationToken ct = default)
    {
        var streak = await _streakLogs.GetByUserIdAsync(query.UserId, ct);
        return new StreakDto(streak.CurrentStreak, streak.LongestStreak, streak.FreezeCount);
    }
}
