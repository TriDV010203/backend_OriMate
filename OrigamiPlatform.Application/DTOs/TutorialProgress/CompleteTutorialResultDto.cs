using OrigamiPlatform.Application.DTOs.Achievements;

namespace OrigamiPlatform.Application.DTOs.TutorialProgress;

public record CompleteTutorialResultDto(
    TutorialProgressDto Progress,
    AchievementDto Achievement,
    bool IsNewCompletion
);
