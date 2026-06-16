namespace OrigamiPlatform.Application.Commands.FamilyProjects;

public record RespondFamilyInvitationCommand(Guid ProjectId, Guid UserId, bool Accept);
