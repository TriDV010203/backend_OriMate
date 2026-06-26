namespace OrigamiPlatform.Application.Queries.FamilyProjects;

public record GetProjectProgressQuery(Guid ProjectId, Guid CurrentUserId);
