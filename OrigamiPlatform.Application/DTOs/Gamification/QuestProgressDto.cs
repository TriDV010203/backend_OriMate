namespace OrigamiPlatform.Application.DTOs.Gamification;

public record QuestProgressDto(string Title, int Progress, int TargetValue, bool IsCompleted);
