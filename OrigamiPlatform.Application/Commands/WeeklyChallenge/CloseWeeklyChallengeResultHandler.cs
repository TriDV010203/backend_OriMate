using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Constants;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Commands.WeeklyChallenge;

// Idempotent — no-ops if the weekly challenge for that date doesn't exist or isn't Active.
// Mirror CloseDailyChallengeResultHandler; rank 1 vẫn cộng FreezeCount vào ChallengeStreakLog
// dùng chung với Thử thách ngày.
public class CloseWeeklyChallengeResultHandler
{
    private readonly IWeeklyChallengeRepository _challenges;
    private readonly IWeeklyChallengeSubmissionRepository _submissions;
    private readonly ILikeRepository _likes;
    private readonly IChallengeStreakRepository _challengeStreaks;
    private readonly HatGapAwardService _hatGap;
    private readonly BadgeAwardService _badges;
    private readonly INotificationService _notifications;

    public CloseWeeklyChallengeResultHandler(
        IWeeklyChallengeRepository challenges,
        IWeeklyChallengeSubmissionRepository submissions,
        ILikeRepository likes,
        IChallengeStreakRepository challengeStreaks,
        HatGapAwardService hatGap,
        BadgeAwardService badges,
        INotificationService notifications)
        => (_challenges, _submissions, _likes, _challengeStreaks, _hatGap, _badges, _notifications)
            = (challenges, submissions, likes, challengeStreaks, hatGap, badges, notifications);

    public async Task HandleAsync(CloseWeeklyChallengeResultCommand command, CancellationToken ct = default)
    {
        var challenge = await _challenges.GetByDateAsync(command.ChallengeDate, ct);
        if (challenge is null || challenge.Status != DailyChallengeStatus.Active)
            return;

        var submissions = await _submissions.GetByChallengeAsync(challenge.Id, ct);

        if (submissions.Count > 0)
        {
            var likeCounts = await _likes.GetCountsForTargetsAsync(
                submissions.Select(s => s.Id), TargetType.WeeklyChallengeSubmission);

            var ranked = submissions
                .OrderByDescending(s => likeCounts.GetValueOrDefault(s.Id, 0))
                .ThenBy(s => s.CreatedAt)
                .ToList();

            for (var i = 0; i < ranked.Count; i++)
                ranked[i].FinalRank = i + 1;

            await _submissions.UpdateRangeAsync(ranked, ct);

            var top = ranked.Take(3).ToList();
            for (var i = 0; i < top.Count; i++)
                await RewardRankAsync(challenge, top[i], i + 1, ct);
        }

        challenge.Status = DailyChallengeStatus.Closed;
        challenge.ResultCalculatedAt = DateTime.UtcNow;
        challenge.UpdatedAt = DateTime.UtcNow;
        await _challenges.UpdateAsync(challenge, ct);
    }

    private async Task RewardRankAsync(
        OrigamiPlatform.Domain.Entities.WeeklyChallenge challenge, WeeklyChallengeSubmission submission, int rank, CancellationToken ct)
    {
        try
        {
            var reward = HatGapEconomy.WeeklyChallengeRankReward[rank];
            await _hatGap.AwardAsync(submission.UserId, reward, HatGapTransactionType.Earn, $"WeeklyChallengeRank{rank}", ct);

            await _notifications.NotifyUserAsync(
                userId: submission.UserId,
                type: NotificationType.DailyChallengeResult,
                message: $"Chúc mừng! Bài nộp của bạn xếp hạng {rank} Thử thách tuần {challenge.ChallengeDate:dd/MM/yyyy}. +{reward} Hạt Gấp 🏆",
                entityType: nameof(WeeklyChallengeSubmission),
                entityId: submission.Id,
                ct: ct);

            if (rank == 1)
            {
                await _badges.TryAwardAsync(submission.UserId, "CHALLENGE_RANK1_FIRST", contextRefId: challenge.Id, ct: ct);

                var streak = await _challengeStreaks.GetByUserIdAsync(submission.UserId, ct);
                streak.FreezeCount++;
                await _challengeStreaks.UpdateAsync(streak, ct);
            }

            await _badges.TryAwardAsync(submission.UserId, "CHALLENGE_TOP3_FIRST", contextRefId: challenge.Id, ct: ct);

            var top3Count = await _submissions.CountByUserWithMaxRankAsync(submission.UserId, 3, ct);
            if (top3Count >= 5)
                await _badges.TryAwardAsync(submission.UserId, "CHALLENGE_TOP3_5X", contextRefId: challenge.Id, ct: ct);
        }
        catch
        {
            // rank reward failure for one user must not block closing the challenge or rewarding others
        }
    }
}
