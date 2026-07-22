namespace OrigamiPlatform.Application.Commands.Clan;

public record InviteMemberCommand(Guid RequesterId, Guid ClanId, Guid InviteeUserId);
