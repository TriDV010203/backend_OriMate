using OrigamiPlatform.Application.DTOs.Achievements;

namespace OrigamiPlatform.Application.DTOs.TutorialProgress;

public record CompleteTutorialResultDto(
    AchievementDto Achievement,
    bool IsNewCompletion
);
