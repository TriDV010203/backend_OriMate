namespace OrigamiPlatform.Application.Queries.Tutorials;

public record GetRecommendedTutorialsQuery(Guid? UserId, int Page = 1, int PageSize = 10);
