using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.Achievements;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Constants;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Achievements;

public class CreateAchievementHandler
{
    private const int MaxNoteLength = 500;
    private const int MaxPhotoUrlLength = 512;

    private readonly IAchievementRepository _achievements;
    private readonly IPersonalMilestoneRepository _milestones;
    private readonly INotificationService _notifications;
    private readonly HatGapAwardService _hatGap;
    private readonly ILearningPathRepository _learningPaths;
    private readonly ILearningPathCompletionRepository _pathCompletions;

    public CreateAchievementHandler(
        IAchievementRepository achievements,
        IPersonalMilestoneRepository milestones,
        INotificationService notifications,
        HatGapAwardService hatGap,
        ILearningPathRepository learningPaths,
        ILearningPathCompletionRepository pathCompletions)
        => (_achievements, _milestones, _notifications, _hatGap, _learningPaths, _pathCompletions)
            = (achievements, milestones, notifications, hatGap, learningPaths, pathCompletions);

    public async Task<AchievementDto> HandleAsync(
        CreateAchievementCommand command,
        CancellationToken ct = default)
    {
        Validate(command.Request.PhotoUrl, command.Request.Note);

        var tutorialExists = await _achievements.PublishedTutorialExistsAsync(
            command.Request.TutorialId, ct);
        if (!tutorialExists)
            throw new NotFoundException("Published tutorial not found.");

        var existing = await _achievements.GetByUserAndTutorialAsync(
            command.UserId,
            command.Request.TutorialId,
            ct);
        if (existing is not null)
            throw new DomainException("You already marked this tutorial as completed.");

        var now = DateTime.UtcNow;
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            TutorialId = command.Request.TutorialId,
            PhotoUrl = command.Request.PhotoUrl,
            Note = command.Request.Note,
            IsPublic = command.Request.IsPublic,
            CreatedAt = now
        };

        await _achievements.AddAsync(achievement, ct);

        await UnlockMilestonesAsync(command.UserId, ct);
        await AwardLearningPathCompletionAsync(command.UserId, command.Request.TutorialId, ct);

        var created = await _achievements.GetByIdAsync(achievement.Id, ct)
            ?? throw new NotFoundException("Achievement not found after creation.");

        return created.ToDto();
    }

    private async Task UnlockMilestonesAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var totalCount = await _achievements.CountByUserAsync(userId, ct);

            foreach (var threshold in MilestoneThresholds.Values)
            {
                if (totalCount < threshold)
                    continue;

                if (await _milestones.ExistsAsync(userId, threshold, ct))
                    continue;

                var milestone = new PersonalMilestone
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Threshold = threshold,
                    UnlockedAt = DateTime.UtcNow
                };

                await _milestones.AddAsync(milestone, ct);

                var reward = HatGapEconomy.PersonalMilestoneReward[threshold];
                await _hatGap.AwardAsync(userId, reward, HatGapTransactionType.Earn, $"PersonalMilestone{threshold}", ct);

                await _notifications.NotifyUserAsync(
                    userId: userId,
                    type: NotificationType.MilestoneUnlocked,
                    message: $"Bạn đã mở khoá huy hiệu {threshold} thành tựu! +{reward} Hạt Gấp & 1 Mẫu giấy đặc biệt 🎁",
                    entityType: nameof(PersonalMilestone),
                    entityId: milestone.Id,
                    ct: ct
                );
            }
        }
        catch
        {
            // milestone unlock failure must not affect the main achievement creation flow
        }
    }

    // Awards a one-time Hạt Gấp bonus the moment a user finishes every tutorial in a published Learning Path.
    private async Task AwardLearningPathCompletionAsync(Guid userId, Guid tutorialId, CancellationToken ct)
    {
        try
        {
            var path = await _learningPaths.GetPublishedPathContainingTutorialAsync(tutorialId, ct);
            if (path is null)
                return;

            if (await _pathCompletions.ExistsAsync(userId, path.Id, ct))
                return;

            var completedTutorialIds = await _achievements.GetCompletedTutorialIdsAsync(userId, ct);
            var pathTutorialIds = path.Items.Select(i => i.TutorialId);
            if (!pathTutorialIds.All(completedTutorialIds.Contains))
                return;

            await _pathCompletions.AddAsync(new LearningPathCompletion
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LearningPathId = path.Id,
                CompletedAt = DateTime.UtcNow
            }, ct);

            await _hatGap.AwardAsync(
                userId, HatGapEconomy.LearningPathCompletionReward, HatGapTransactionType.Earn, $"LearningPathComplete_{path.Id}", ct);

            await _notifications.NotifyUserAsync(
                userId: userId,
                type: NotificationType.LearningPathCompleted,
                message: $"Bạn đã hoàn thành lộ trình \"{path.Title}\"! +{HatGapEconomy.LearningPathCompletionReward} Hạt Gấp 🎉",
                entityType: nameof(LearningPath),
                entityId: path.Id,
                ct: ct
            );
        }
        catch
        {
            // learning-path completion reward failure must not affect the main achievement creation flow
        }
    }

    private static void Validate(string? photoUrl, string? note)
    {
        if (photoUrl is { Length: > MaxPhotoUrlLength })
            throw new DomainException($"Photo URL must not exceed {MaxPhotoUrlLength} characters.");

        if (note is { Length: > MaxNoteLength })
            throw new DomainException($"Note must not exceed {MaxNoteLength} characters.");
    }
}
