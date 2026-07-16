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

    public CreateAchievementHandler(
        IAchievementRepository achievements,
        IPersonalMilestoneRepository milestones,
        INotificationService notifications)
        => (_achievements, _milestones, _notifications) = (achievements, milestones, notifications);

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

                await _notifications.NotifyUserAsync(
                    userId: userId,
                    type: NotificationType.MilestoneUnlocked,
                    message: $"Bạn đã mở khoá huy hiệu {threshold} thành tựu!",
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

    private static void Validate(string? photoUrl, string? note)
    {
        if (photoUrl is { Length: > MaxPhotoUrlLength })
            throw new DomainException($"Photo URL must not exceed {MaxPhotoUrlLength} characters.");

        if (note is { Length: > MaxNoteLength })
            throw new DomainException($"Note must not exceed {MaxNoteLength} characters.");
    }
}
