namespace OrigamiPlatform.Application.Queries.Tutorials;

public record GetMyTutorialsQuery(Guid AuthorId, int Page = 1, int PageSize = 12);
