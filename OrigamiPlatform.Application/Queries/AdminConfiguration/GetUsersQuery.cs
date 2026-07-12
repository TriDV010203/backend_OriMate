namespace OrigamiPlatform.Application.Queries.AdminConfiguration;

public record GetUsersQuery(
    string? Keyword,
    string? Status,
    string? Role,
    int Page = 1,
    int PageSize = 20
);
