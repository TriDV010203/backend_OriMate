using OrigamiPlatform.Application.DTOs.Clan;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Clan;

public class CreateClanHandler
{
    private readonly IClanRepository _clans;
    private readonly IClanMemberRepository _members;

    public CreateClanHandler(IClanRepository clans, IClanMemberRepository members)
        => (_clans, _members) = (clans, members);

    public async Task<ClanDto> HandleAsync(CreateClanCommand command, CancellationToken ct = default)
    {
        var existingMembership = await _members.GetByUserIdAsync(command.UserId, ct);
        if (existingMembership is not null)
            throw new DomainException("You are already in a clan.");

        var existingClan = await _clans.GetByNameAsync(command.Name, ct);
        if (existingClan is not null)
            throw new DomainException("This clan name is already taken.");

        var now = DateTime.UtcNow;
        var clan = new Domain.Entities.Clan
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            OwnerId = command.UserId,
            CreatedAt = now
        };
        await _clans.AddAsync(clan, ct);

        var member = new ClanMember
        {
            Id = Guid.NewGuid(),
            ClanId = clan.Id,
            UserId = command.UserId,
            JoinedAt = now,
            ContributionPoints = 0
        };
        await _members.AddAsync(member, ct);

        var members = await _members.GetByClanIdAsync(clan.Id, ct);
        return clan.ToDto(members);
    }
}
