using OrigamiPlatform.Application.DTOs.DailyChallenge;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.DailyChallenge;

public class GetChallengeStreakLeaderboardHandler
{
    private readonly IChallengeStreakRepository _challengeStreaks;

    public GetChallengeStreakLeaderboardHandler(IChallengeStreakRepository challengeStreaks)
        => _challengeStreaks = challengeStreaks;

    public async Task<List<ChallengeLeaderboardEntryDto>> HandleAsync(
        GetChallengeStreakLeaderboardQuery query, CancellationToken ct = default)
    {
        var top = Math.Clamp(query.Top, 1, 100);
        var streaks = await _challengeStreaks.GetTopAsync(top, ct);

        return streaks
            .Select((s, i) => new ChallengeLeaderboardEntryDto(
                i + 1, s.UserId, s.User.Profile?.DisplayName, s.User.Profile?.AvatarUrl,
                s.CurrentStreak, s.LongestStreak))
            .ToList();
    }
}
