namespace OrigamiPlatform.Application.Queries.Tutorials;

public record GetTutorialsQuery(
    string? Search = null,
    int? CategoryId = null,
    string? Difficulty = null,
    string? Type = null,
    string? SortBy = null,
    int Page = 1,
    int PageSize = 20,
    Guid? CurrentUserId = null
);
