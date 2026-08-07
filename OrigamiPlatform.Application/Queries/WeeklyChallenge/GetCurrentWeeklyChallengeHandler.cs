using OrigamiPlatform.Application.DTOs.WeeklyChallenge;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.WeeklyChallenge;

public class GetCurrentWeeklyChallengeHandler
{
    private readonly IWeeklyChallengeRepository _challenges;
    private readonly IWeeklyChallengeSubmissionRepository _submissions;
    private readonly IChallengeStreakRepository _challengeStreaks;

    public GetCurrentWeeklyChallengeHandler(
        IWeeklyChallengeRepository challenges,
        IWeeklyChallengeSubmissionRepository submissions,
        IChallengeStreakRepository challengeStreaks)
        => (_challenges, _submissions, _challengeStreaks) = (challenges, submissions, challengeStreaks);

    public async Task<WeeklyChallengeDto> HandleAsync(GetCurrentWeeklyChallengeQuery query, CancellationToken ct = default)
    {
        var today = GetTodayGmt7();
        var challenge = await _challenges.GetByDateAsync(today, ct)
            ?? throw new NotFoundException("Hiện chưa có Thử thách tuần — chỉ mở vào Chủ Nhật.");

        var submissions = await _submissions.GetByChallengeAsync(challenge.Id, ct);

        bool? hasSubmitted = null;
        int? myStreak = null;

        if (query.CurrentUserId.HasValue)
        {
            hasSubmitted = submissions.Any(s => s.UserId == query.CurrentUserId.Value);
            var streak = await _challengeStreaks.GetByUserIdAsync(query.CurrentUserId.Value, ct);
            myStreak = streak.CurrentStreak;
        }

        return challenge.ToDto(submissions.Count, hasSubmitted, myStreak);
    }

    private static DateOnly GetTodayGmt7() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
}
