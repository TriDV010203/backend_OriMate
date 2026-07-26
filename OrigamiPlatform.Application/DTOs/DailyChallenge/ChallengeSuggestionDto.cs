namespace OrigamiPlatform.Application.DTOs.DailyChallenge;

public record ChallengeSuggestionDto(
    Guid TutorialId,
    string Title,
    string Slug,
    string? CoverImageUrl,
    string Difficulty,
    int CategoryId,
    int AchievementCount
);
