using OrigamiPlatform.Application.DTOs.Clan;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Clan;

public class TransferOwnershipHandler
{
    private readonly IClanRepository _clans;
    private readonly IClanMemberRepository _members;

    public TransferOwnershipHandler(IClanRepository clans, IClanMemberRepository members)
        => (_clans, _members) = (clans, members);

    public async Task<ClanDto> HandleAsync(TransferOwnershipCommand command, CancellationToken ct = default)
    {
        var clan = await _clans.GetByIdAsync(command.ClanId, ct)
            ?? throw new NotFoundException("Clan not found.");

        if (clan.OwnerId != command.RequesterId)
            throw new ForbiddenException("Only the Clan Owner can transfer ownership.");

        if (command.NewOwnerId == command.RequesterId)
            throw new DomainException("Cannot transfer ownership to yourself.");

        var newOwnerMembership = await _members.GetByUserIdAsync(command.NewOwnerId, ct);
        if (newOwnerMembership is null || newOwnerMembership.ClanId != command.ClanId)
            throw new DomainException("The new owner must be a member of this Clan.");

        clan.OwnerId = command.NewOwnerId;
        await _clans.UpdateAsync(clan, ct);

        var members = await _members.GetByClanIdAsync(clan.Id, ct);
        return clan.ToDto(members);
    }
}
