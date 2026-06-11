using OrigamiPlatform.Application.DTOs.Achievements;

namespace OrigamiPlatform.Application.Commands.Achievements;

public record CreateAchievementCommand(Guid UserId, CreateAchievementRequest Request);
