namespace OrigamiPlatform.Application.Queries.Tutorials;

public record GetTutorialBySlugQuery(string Slug, Guid? CurrentUserId = null);
