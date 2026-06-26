namespace OrigamiPlatform.Application.Commands.FamilyProjects;

public record ShareProjectCommand(Guid ProjectId, Guid UserId, string? Note);
