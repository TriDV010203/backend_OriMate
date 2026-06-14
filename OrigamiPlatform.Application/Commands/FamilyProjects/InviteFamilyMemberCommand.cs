namespace OrigamiPlatform.Application.Commands.FamilyProjects;

public record InviteFamilyMemberCommand(Guid ProjectId, Guid RequesterId, string Email);
