using OrigamiPlatform.Application.DTOs.DailyChallenge;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.DailyChallenge;

public class GetTodayChallengeHandler
{
    private readonly IDailyChallengeRepository _challenges;
    private readonly IDailyChallengeSubmissionRepository _submissions;
    private readonly IChallengeStreakRepository _challengeStreaks;

    public GetTodayChallengeHandler(
        IDailyChallengeRepository challenges,
        IDailyChallengeSubmissionRepository submissions,
        IChallengeStreakRepository challengeStreaks)
        => (_challenges, _submissions, _challengeStreaks) = (challenges, submissions, challengeStreaks);

    public async Task<DailyChallengeDto> HandleAsync(GetTodayChallengeQuery query, CancellationToken ct = default)
    {
        var today = GetTodayGmt7();
        var challenge = await _challenges.GetByDateAsync(today, ct)
            ?? throw new NotFoundException("Hôm nay chưa có Thử thách ngày.");

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
