using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.DTOs.Achievements;

public static class AchievementMapping
{
    public static AchievementDto ToDto(this Achievement achievement)
        => new(
            achievement.Id,
            achievement.UserId,
            achievement.TutorialId,
            achievement.Tutorial.Title,
            achievement.Tutorial.Slug,
            achievement.PhotoUrl,
            achievement.Note,
            achievement.IsPublic,
            achievement.CreatedAt,
            achievement.UpdatedAt);
}
