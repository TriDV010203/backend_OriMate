namespace OrigamiPlatform.Application.DTOs.Clan;

public record ClanMemberDto(Guid UserId, string DisplayName, DateTime JoinedAt);
