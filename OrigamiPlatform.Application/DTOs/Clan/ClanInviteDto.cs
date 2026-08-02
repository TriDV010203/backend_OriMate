namespace OrigamiPlatform.Application.DTOs.Clan;

public record ClanInviteDto(Guid Id, Guid ClanId, string ClanName, DateTime ExpiresAt, DateTime CreatedAt);
