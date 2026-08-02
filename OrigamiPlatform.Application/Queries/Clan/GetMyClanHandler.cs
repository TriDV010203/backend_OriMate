using OrigamiPlatform.Application.DTOs.Clan;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Clan;

public class GetMyClanHandler
{
    private readonly IClanMemberRepository _members;

    public GetMyClanHandler(IClanMemberRepository members) => _members = members;

    public async Task<ClanDto?> HandleAsync(GetMyClanQuery query, CancellationToken ct = default)
    {
        var membership = await _members.GetByUserIdAsync(query.UserId, ct);
        if (membership is null)
            return null;

        var members = await _members.GetByClanIdAsync(membership.ClanId, ct);
        return membership.Clan.ToDto(members);
    }
}
