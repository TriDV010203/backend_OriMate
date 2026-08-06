namespace OrigamiPlatform.Application.DTOs.LearningPathModes;

public record CreateLearningPathModeRequest(string Name, string? Description, int SortOrder);
