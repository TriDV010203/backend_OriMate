namespace OrigamiPlatform.Application.Queries.LearningPathModes;

/// <summary>Admin review queue for mode-unlock test submissions.</summary>
public record GetModeUnlockSubmissionsQuery(Guid? ModeId, string? Status, int Page, int PageSize);
