using OrigamiPlatform.Application.DTOs.Clan;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Clan;

public class GetPendingInvitesHandler
{
    private readonly IClanInviteRepository _invites;

    public GetPendingInvitesHandler(IClanInviteRepository invites) => _invites = invites;

    public async Task<List<ClanInviteDto>> HandleAsync(GetPendingInvitesQuery query, CancellationToken ct = default)
    {
        var invites = await _invites.GetPendingByUserIdAsync(query.UserId, ct);

        return invites
            .Where(i => i.ExpiresAt > DateTime.UtcNow)
            .Select(i => i.ToDto())
            .ToList();
    }
}
