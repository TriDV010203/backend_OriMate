using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Common;

// Streak tham gia Thử thách — dùng chung cho cả Thử thách ngày và Thử thách tuần, nên nộp bài
// ở loại nào trong tuần (6 ngày Thử thách ngày + Chủ Nhật Thử thách tuần) cũng nối tiếp cùng
// một chuỗi. Mirrors StreakLog's freeze rule but on ChallengeStreakLog — độc lập hoàn toàn với
// streak học tập chung (FT-26).
public class ChallengeStreakService
{
    private readonly IChallengeStreakRepository _challengeStreaks;
    private readonly BadgeAwardService _badges;

    public ChallengeStreakService(IChallengeStreakRepository challengeStreaks, BadgeAwardService badges)
        => (_challengeStreaks, _badges) = (challengeStreaks, badges);

    /// <summary>Records today's challenge participation for the streak, awards streak-milestone badges, and
    /// returns the resulting CurrentStreak. Never throws — failures are swallowed so the caller's main
    /// submission flow is never blocked by a streak-tracking bug.</summary>
    public async Task<int?> UpdateAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var streak = await _challengeStreaks.GetByUserIdAsync(userId, ct);
            var today = GetTodayGmt7();

            if (streak.LastSubmissionDate == today)
            {
                // already counted today — nothing to change
            }
            else if (streak.LastSubmissionDate == today.AddDays(-1))
            {
                streak.CurrentStreak++;
                streak.LastSubmissionDate = today;
            }
            else if (streak.LastSubmissionDate == today.AddDays(-2) && streak.FreezeCount > 0)
            {
                streak.FreezeCount--;
                streak.CurrentStreak++;
                streak.LastSubmissionDate = today;
            }
            else
            {
                streak.CurrentStreak = 1;
                streak.LastSubmissionDate = today;
            }

            if (streak.CurrentStreak > streak.LongestStreak)
                streak.LongestStreak = streak.CurrentStreak;

            await _challengeStreaks.UpdateAsync(streak, ct);

            if (streak.CurrentStreak is 3 or 7 or 30)
                await _badges.TryAwardAsync(userId, $"STREAK_CHALLENGE_{streak.CurrentStreak}", ct: ct);

            return streak.CurrentStreak;
        }
        catch
        {
            // streak update failure must not affect the main submission flow
            return null;
        }
    }

    private static DateOnly GetTodayGmt7() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
}
