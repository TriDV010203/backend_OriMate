using OrigamiPlatform.Application.DTOs.Achievements;

namespace OrigamiPlatform.Application.Commands.Achievements;

public record UpdateAchievementCommand(
    Guid UserId,
    Guid AchievementId,
    UpdateAchievementRequest Request);
