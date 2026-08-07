namespace OrigamiPlatform.Application.DTOs.WeeklyChallenge;

public record WeeklyChallengeSuggestionDto(
    Guid TutorialId,
    string Title,
    string Slug,
    string? CoverImageUrl,
    string Difficulty,
    int CategoryId,
    int AchievementCount
);
