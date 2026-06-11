namespace OrigamiPlatform.Application.Commands.Achievements;

public record DeleteAchievementCommand(Guid UserId, Guid AchievementId);
