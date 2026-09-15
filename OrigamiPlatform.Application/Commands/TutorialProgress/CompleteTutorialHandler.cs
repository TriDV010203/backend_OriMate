using OrigamiPlatform.Application.Commands.Achievements;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.Achievements;
using OrigamiPlatform.Application.DTOs.TutorialProgress;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Constants;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.TutorialProgress;

public class CompleteTutorialHandler
{
    // BR-SKILL-01: points awarded per tutorial completed, by difficulty
    private static readonly Dictionary<TutorialDifficulty, int> SkillPointsByDifficulty = new()
    {
        [TutorialDifficulty.Beginner] = 1,
        [TutorialDifficulty.Intermediate] = 2,
        [TutorialDifficulty.Advanced] = 3
    };

    private readonly ITutorialRepository _tutorials;
    private readonly IAchievementRepository _achievements;
    private readonly CreateAchievementHandler _createAchievement;
    private readonly ITutorialDifficultyRatingRepository _ratings;
    private readonly IUserRepository _users;
    private readonly IStreakLogRepository _streakLogs;
    private readonly HatGapAwardService _hatGap;
    private readonly INotificationService _notifications;
    private readonly BadgeAwardService _badges;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteTutorialHandler(
        ITutorialRepository tutorials,
        IAchievementRepository achievements,
        CreateAchievementHandler createAchievement,
        ITutorialDifficultyRatingRepository ratings,
        IUserRepository users,
        IStreakLogRepository streakLogs,
        HatGapAwardService hatGap,
        INotificationService notifications,
        BadgeAwardService badges,
        IUnitOfWork unitOfWork)
        => (_tutorials, _achievements, _createAchievement, _ratings, _users, _streakLogs, _hatGap, _notifications, _badges, _unitOfWork)
            = (tutorials, achievements, createAchievement, ratings, users, streakLogs, hatGap, notifications, badges, unitOfWork);

    public async Task<CompleteTutorialResultDto> HandleAsync(
        CompleteTutorialCommand command, CancellationToken ct = default)
    {
        var tutorial = await _tutorials.GetByIdWithStepsAsync(command.TutorialId, ct);
        if (tutorial is null || tutorial.Status != TutorialStatus.Published)
            throw new NotFoundException("Published tutorial not found.");

        if (tutorial.Steps.Count == 0)
            throw new DomainException("This tutorial has no steps.");

        // Redo case: an Achievement already exists for this user+tutorial -> behave like a normal replay,
        // but never grant a second Achievement, a second difficulty rating, or re-fire rewards.
        var existingAchievement = await _achievements.GetByUserAndTutorialAsync(command.UserId, command.TutorialId, ct);
        if (existingAchievement is not null)
            return new CompleteTutorialResultDto(existingAchievement.ToDto(), IsNewCompletion: false);

        AchievementDto? achievementDto = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            achievementDto = await _createAchievement.HandleAsync(
                new CreateAchievementCommand(
                    command.UserId,
                    new CreateAchievementRequest(command.TutorialId, command.PhotoUrl, command.Note, command.IsPublic)),
                ct);

            if (command.PerceivedDifficulty.HasValue
                && !await _ratings.ExistsAsync(command.UserId, command.TutorialId, ct))
            {
                await _ratings.AddAsync(new TutorialDifficultyRating
                {
                    Id = Guid.NewGuid(),
                    UserId = command.UserId,
                    TutorialId = command.TutorialId,
                    Rating = command.PerceivedDifficulty.Value,
                    CreatedAt = DateTime.UtcNow
                }, ct);
            }
        }, ct);

        // First-time completion rewards — the Achievement existence check above guarantees this
        // fires exactly once per user per tutorial (BR-25/BR-26/BR-28: skill points, Hạt Gấp, streak).
        await AwardSkillPointsAsync(command.UserId, tutorial.Difficulty, ct);
        await AwardTutorialCompletionHatGapAsync(command.UserId, tutorial.Difficulty, ct);
        await UpdateStreakAsync(command.UserId, ct);

        return new CompleteTutorialResultDto(achievementDto!, IsNewCompletion: true);
    }

    // FT-25: tutorial just completed for the first time — award skill points and recompute SkillLevel.
    // Never let a skill-point failure fail the main completion flow.
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
            // skill point award failure must not affect the main completion flow
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
            // Hạt Gấp award failure must not affect the main completion flow
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
            // streak update failure must not affect the main completion flow
        }
    }

    // Streak milestone bonus — fires whenever CurrentStreak lands exactly on a milestone day count
    // (naturally once per streak run since CurrentStreak only increments by 1 at a time).
    private async Task AwardStreakMilestoneAsync(Guid userId, int currentStreak, CancellationToken ct)
    {
        if (!HatGapEconomy.StreakMilestoneReward.TryGetValue(currentStreak, out var reward))
            return;

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

    private static DateOnly GetTodayGmt7() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
}
