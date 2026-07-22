using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Clan;

public class LeaveClanHandler
{
    private readonly IClanMemberRepository _members;

    public LeaveClanHandler(IClanMemberRepository members) => _members = members;

    public async Task HandleAsync(LeaveClanCommand command, CancellationToken ct = default)
    {
        var member = await _members.GetByUserIdAsync(command.UserId, ct);
        if (member is null || member.ClanId != command.ClanId)
            throw new NotFoundException("You are not a member of this clan.");

        if (member.Clan.OwnerId == command.UserId)
            throw new DomainException("Clan Owner must transfer ownership before leaving.");

        await _members.DeleteAsync(member, ct);
    }
}
