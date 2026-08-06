namespace OrigamiPlatform.Application.Queries.LearningPaths;

/// <summary>Public browsing: Published learning paths only.</summary>
public record GetLearningPathsQuery(string? Search, Guid? ModeId, int Page, int PageSize);
