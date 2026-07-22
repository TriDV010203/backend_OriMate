using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.DTOs.Clan;

public static class ClanMapping
{
    public static ClanDto ToDto(this Domain.Entities.Clan clan, List<ClanMember> members)
        => new(
            clan.Id,
            clan.Name,
            clan.OwnerId,
            clan.CreatedAt,
            members.Select(m => m.ToDto()).ToList());

    public static ClanMemberDto ToDto(this ClanMember member)
        => new(
            member.UserId,
            member.User.Profile?.DisplayName ?? member.User.Email,
            member.JoinedAt);

    public static ClanInviteDto ToDto(this ClanInvite invite)
        => new(
            invite.Id,
            invite.ClanId,
            invite.Clan.Name,
            invite.ExpiresAt,
            invite.CreatedAt);
}
