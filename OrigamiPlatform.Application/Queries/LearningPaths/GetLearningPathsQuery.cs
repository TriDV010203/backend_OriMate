namespace OrigamiPlatform.Application.Queries.LearningPaths;

/// <summary>Public browsing: Published learning paths only.</summary>
public record GetLearningPathsQuery(string? Search, int Page, int PageSize);
