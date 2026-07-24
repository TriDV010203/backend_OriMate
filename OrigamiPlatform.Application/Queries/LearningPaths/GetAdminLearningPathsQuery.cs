namespace OrigamiPlatform.Application.Queries.LearningPaths;

/// <summary>Admin management list: every learning path, any status.</summary>
public record GetAdminLearningPathsQuery(string? Search, string? Status, int Page, int PageSize);
