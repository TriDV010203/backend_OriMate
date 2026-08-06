namespace OrigamiPlatform.Application.Commands.Clan;

public record TransferOwnershipCommand(Guid RequesterId, Guid ClanId, Guid NewOwnerId);
