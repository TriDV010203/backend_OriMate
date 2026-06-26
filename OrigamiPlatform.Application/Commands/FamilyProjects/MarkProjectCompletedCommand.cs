namespace OrigamiPlatform.Application.Commands.FamilyProjects;

public record MarkProjectCompletedCommand(Guid ProjectId, Guid UserId);
