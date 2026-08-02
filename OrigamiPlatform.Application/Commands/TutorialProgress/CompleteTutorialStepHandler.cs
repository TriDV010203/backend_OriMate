using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.TutorialProgress;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Constants;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.TutorialProgress;

public class CompleteTutorialStepHandler
{
    // BR-SKILL-01: points awarded per tutorial completed, by difficulty
    private static readonly Dictionary<TutorialDifficulty, int> SkillPointsByDifficulty = new()
    {
        [TutorialDifficulty.Beginner] = 1,
        [TutorialDifficulty.Intermediate] = 2,
        [TutorialDifficulty.Advanced] = 3
    };

    private readonly ITutorialStepProgressRepository _progress;
    private readonly IUserRepository _users;
    private readonly IStreakLogRepository _streakLogs;
    private readonly IDailyQuestRepository _dailyQuests;
    private readonly IUserDailyQuestProgressRepository _questProgress;
    private readonly HatGapAwardService _hatGap;
    private readonly INotificationService _notifications;
    private readonly BadgeAwardService _badges;

    public CompleteTutorialStepHandler(
        ITutorialStepProgressRepository progress,
        IUserRepository users,
        IStreakLogRepository streakLogs,
        IDailyQuestRepository dailyQuests,
        IUserDailyQuestProgressRepository questProgress,
        HatGapAwardService hatGap,
        INotificationService notifications,
        BadgeAwardService badges)
        => (_progress, _users, _streakLogs, _dailyQuests, _questProgress, _hatGap, _notifications, _badges)
            = (progress, users, streakLogs, dailyQuests, questProgress, hatGap, notifications, badges);

    public async Task<TutorialProgressDto> HandleAsync(
        CompleteTutorialStepCommand command, CancellationToken ct = default)
    {
        var step = await _progress.GetStepWithTutorialAsync(command.StepId, ct)
            ?? throw new NotFoundException("Tutorial step not found.");

        if (step.TutorialId != command.TutorialId)
            throw new NotFoundException("This step does not belong to the given tutorial.");

        if (step.Tutorial.Status != TutorialStatus.Published || step.Tutorial.IsDeleted)
            throw new DomainException("You can only track progress on a published tutorial.");

        // Each user can complete a step only once.
        if (await _progress.ExistsAsync(command.UserId, command.StepId, ct))
            throw new DomainException("You have already completed this step.");

        await _progress.AddAsync(new TutorialStepProgress
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            TutorialId = step.TutorialId,
            TutorialStepId = command.StepId,
            CompletedAt = DateTime.UtcNow
        }, ct);

        var total = await _progress.CountStepsAsync(command.TutorialId, ct);
        var completedIds = await _progress.GetCompletedStepIdsAsync(command.UserId, command.TutorialId, ct);

        if (total > 0 && completedIds.Count >= total)
        {
            await AwardSkillPointsAsync(command.UserId, step.Tutorial.Difficulty, ct);
            await AwardTutorialCompletionHatGapAsync(command.UserId, step.Tutorial.Difficulty, ct);
        }

        // FT-26 / FT-27: streak and daily quest only track tutorial-step completions
        // (not comments/posts) — fired on every step, independent of full-tutorial completion above.
        await UpdateStreakAsync(command.UserId, ct);
        await UpdateQuestProgressAsync(command.UserId, ct);

        return TutorialProgressFactory.Create(command.TutorialId, total, completedIds);
    }

    // FT-25: tutorial just completed for the first time (steps can only be completed once each,
    // so this fires exactly once per user per tutorial) — award skill points and recompute SkillLevel.
    // Never let a skill-point failure fail the main complete-step flow.
    private async Task AwardSkillPointsAsync(Guid userId, TutorialDifficulty difficulty, CancellationToken ct)
    {
        try
        {
            var user = await _users.GetByIdAsync(userId, ct);
            if (user?.Profile is null)
                return;

            user.Profile.SkillPoints += SkillPointsByDifficulty[difficulty];
            user.Profile.SkillLevel = user.Profile.SkillPoints switch
            {
                >= SkillLevelThresholds.Advanced => SkillLevel.Advanced,
                >= SkillLevelThresholds.Intermediate => SkillLevel.Intermediate,
                _ => SkillLevel.Beginner
            };

            await _users.UpdateAsync(user, ct);
        }
        catch
        {
            // skill point award failure must not affect the main step-completion flow
        }
    }

    // Every tutorial grants Hạt Gấp on first-time completion, fixed by difficulty (no streak/FFD multiplier).
    private async Task AwardTutorialCompletionHatGapAsync(Guid userId, TutorialDifficulty difficulty, CancellationToken ct)
    {
        try
        {
            var reward = HatGapEconomy.TutorialCompletionReward[difficulty];
            await _hatGap.AwardAsync(userId, reward, HatGapTransactionType.Earn, "TutorialComplete", ct);
        }
        catch
        {
            // Hạt Gấp award failure must not affect the main step-completion flow
        }
    }

    // FT-26: BR-SEEDS-02 auto-consumes a Freeze when exactly one day was missed and a Freeze is available.
    private async Task UpdateStreakAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var streak = await _streakLogs.GetByUserIdAsync(userId, ct);
            var today = GetTodayGmt7();

            if (streak.LastActiveDate == today)
            {
                // already counted today — nothing to change
            }
            else if (streak.LastActiveDate == today.AddDays(-1))
            {
                streak.CurrentStreak++;
                streak.LastActiveDate = today;
            }
            else if (streak.LastActiveDate == today.AddDays(-2) && streak.FreezeCount > 0)
            {
                streak.FreezeCount--;
                streak.CurrentStreak++;
                streak.LastActiveDate = today;
            }
            else
            {
                // missed 2+ days with no Freeze available, or first-ever activity
                streak.CurrentStreak = 1;
                streak.LastActiveDate = today;
            }

            if (streak.CurrentStreak > streak.LongestStreak)
                streak.LongestStreak = streak.CurrentStreak;

            await _streakLogs.UpdateAsync(streak, ct);
            await AwardStreakMilestoneAsync(userId, streak.CurrentStreak, ct);
        }
        catch
        {
            // streak update failure must not affect the main step-completion flow
        }
    }

    // Streak milestone bonus — fires whenever CurrentStreak lands exactly on a milestone day count
    // (naturally once per streak run since CurrentStreak only increments by 1 at a time).
    private async Task AwardStreakMilestoneAsync(Guid userId, int currentStreak, CancellationToken ct)
    {
        if (!HatGapEconomy.StreakMilestoneReward.TryGetValue(currentStreak, out var reward))
            return;

        try
        {
            await _hatGap.AwardAsync(userId, reward, HatGapTransactionType.Earn, $"StreakMilestone{currentStreak}", ct);
            await _notifications.NotifyUserAsync(
                userId: userId,
                type: NotificationType.StreakMilestoneReached,
                message: $"Chuỗi {currentStreak} ngày liên tiếp! Bạn nhận được +{reward} Hạt Gấp 🔥",
                entityType: nameof(StreakLog),
                entityId: userId,
                ct: ct);

            // FT-35: badge catalog mirrors the same 7/14/30-day thresholds
            await _badges.TryAwardAsync(userId, $"STREAK_LEARNING_{currentStreak}", ct: ct);
        }
        catch
        {
            // Hạt Gấp award failure must not affect the main step-completion flow
        }
    }

    // FT-27: Daily Quest bonus = base reward × streak multiplier, capped at ×1.5 on Free Fold Day (Sunday).
    private async Task UpdateQuestProgressAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var quest = (await _dailyQuests.GetActiveAsync(ct)).FirstOrDefault();
            if (quest is null)
                return;

            var today = GetTodayGmt7();
            var progress = await _questProgress.GetOrCreateAsync(userId, quest.Id, today, ct);
            if (progress.IsCompleted)
                return;

            progress.Progress++;
            if (progress.Progress >= quest.TargetValue)
            {
                progress.IsCompleted = true;

                var streak = await _streakLogs.GetByUserIdAsync(userId, ct);
                var isFreeFoldDay = today.DayOfWeek == DayOfWeek.Sunday;
                var multiplier = HatGapEconomy.GetStreakMultiplier(streak.CurrentStreak, isFreeFoldDay);
                var reward = (int)Math.Round(HatGapEconomy.DailyQuestBaseReward * multiplier, MidpointRounding.AwayFromZero);

                await _hatGap.AwardAsync(userId, reward, HatGapTransactionType.Earn, "DailyQuestBonus", ct);
            }

            await _questProgress.UpdateAsync(progress, ct);
        }
        catch
        {
            // quest progress update failure must not affect the main step-completion flow
        }
    }

    private static DateOnly GetTodayGmt7() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
}
