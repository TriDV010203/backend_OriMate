using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Clan;

public class InviteMemberHandler
{
    private readonly IClanRepository _clans;
    private readonly IClanMemberRepository _members;
    private readonly IClanInviteRepository _invites;

    public InviteMemberHandler(
        IClanRepository clans,
        IClanMemberRepository members,
        IClanInviteRepository invites)
        => (_clans, _members, _invites) = (clans, members, invites);

    public async Task HandleAsync(InviteMemberCommand command, CancellationToken ct = default)
    {
        var clan = await _clans.GetByIdAsync(command.ClanId, ct)
            ?? throw new NotFoundException("Clan not found.");

        if (clan.OwnerId != command.RequesterId)
            throw new ForbiddenException("Only the clan owner can invite members.");

        var existingMembership = await _members.GetByUserIdAsync(command.InviteeUserId, ct);
        if (existingMembership is not null)
            throw new DomainException("This user is already in a clan.");

        var invite = new ClanInvite
        {
            Id = Guid.NewGuid(),
            ClanId = clan.Id,
            UserId = command.InviteeUserId,
            Status = ClanInviteStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddHours(48),
            CreatedAt = DateTime.UtcNow
        };

        await _invites.AddAsync(invite, ct);
    }
}
