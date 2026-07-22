using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Clan;

public class AcceptInviteHandler
{
    private readonly IClanInviteRepository _invites;
    private readonly IClanMemberRepository _members;

    public AcceptInviteHandler(IClanInviteRepository invites, IClanMemberRepository members)
        => (_invites, _members) = (invites, members);

    public async Task HandleAsync(AcceptInviteCommand command, CancellationToken ct = default)
    {
        var invite = await _invites.GetByIdAsync(command.InviteId, ct)
            ?? throw new NotFoundException("Invite not found.");

        if (invite.UserId != command.UserId)
            throw new ForbiddenException("This invite does not belong to you.");

        if (invite.Status != ClanInviteStatus.Pending)
            throw new DomainException("This invite has already been processed.");

        if (invite.ExpiresAt <= DateTime.UtcNow)
        {
            invite.Status = ClanInviteStatus.Expired;
            await _invites.UpdateAsync(invite, ct);
            throw new DomainException("This invite has expired.");
        }

        var existingMembership = await _members.GetByUserIdAsync(command.UserId, ct);
        if (existingMembership is not null)
            throw new DomainException("You are already in a clan.");

        var member = new ClanMember
        {
            Id = Guid.NewGuid(),
            ClanId = invite.ClanId,
            UserId = command.UserId,
            JoinedAt = DateTime.UtcNow,
            ContributionPoints = 0
        };
        await _members.AddAsync(member, ct);

        invite.Status = ClanInviteStatus.Accepted;
        await _invites.UpdateAsync(invite, ct);
    }
}
