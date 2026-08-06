namespace OrigamiPlatform.Application.Queries.LearningPathModes;

/// <summary>Public roadmap tabs. CurrentUserId is null for anonymous visitors — only the entry
/// mode shows as unlocked and no personal submission status is included.</summary>
public record GetLearningPathModesQuery(Guid? CurrentUserId);
