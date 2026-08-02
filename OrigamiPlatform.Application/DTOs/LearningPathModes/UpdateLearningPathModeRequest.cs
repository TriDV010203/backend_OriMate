namespace OrigamiPlatform.Application.DTOs.LearningPathModes;

public record UpdateLearningPathModeRequest(string Name, string? Description, int SortOrder, bool IsActive);
