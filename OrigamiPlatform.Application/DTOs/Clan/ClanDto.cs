namespace OrigamiPlatform.Application.DTOs.Clan;

public record ClanDto(Guid Id, string Name, Guid OwnerId, DateTime CreatedAt, List<ClanMemberDto> Members);
