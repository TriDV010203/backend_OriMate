using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Clan;
using OrigamiPlatform.Application.DTOs.Clan;
using OrigamiPlatform.Application.Queries.Clan;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/clans")]
[Authorize]
public class ClanController : ControllerBase
{
    private readonly CreateClanHandler _createClan;
    private readonly InviteMemberHandler _inviteMember;
    private readonly AcceptInviteHandler _acceptInvite;
    private readonly LeaveClanHandler _leaveClan;
    private readonly TransferOwnershipHandler _transferOwnership;
    private readonly GetMyClanHandler _getMyClan;
    private readonly GetPendingInvitesHandler _getPendingInvites;

    public ClanController(
        CreateClanHandler createClan,
        InviteMemberHandler inviteMember,
        AcceptInviteHandler acceptInvite,
        LeaveClanHandler leaveClan,
        TransferOwnershipHandler transferOwnership,
        GetMyClanHandler getMyClan,
        GetPendingInvitesHandler getPendingInvites)
        => (_createClan, _inviteMember, _acceptInvite, _leaveClan, _transferOwnership, _getMyClan, _getPendingInvites)
            = (createClan, inviteMember, acceptInvite, leaveClan, transferOwnership, getMyClan, getPendingInvites);

    [HttpPost]
    public async Task<IActionResult> Create(CreateClanRequest request, CancellationToken ct)
    {
        var result = await _createClan.HandleAsync(
            new CreateClanCommand(GetCurrentUserId(), request.Name),
            ct);

        return Ok(result);
    }

    [HttpPost("{clanId:guid}/invites")]
    public async Task<IActionResult> InviteMember(Guid clanId, InviteMemberRequest request, CancellationToken ct)
    {
        await _inviteMember.HandleAsync(
            new InviteMemberCommand(GetCurrentUserId(), clanId, request.InviteeUserId),
            ct);

        return NoContent();
    }

    [HttpPost("invites/{inviteId:guid}/accept")]
    public async Task<IActionResult> AcceptInvite(Guid inviteId, CancellationToken ct)
    {
        await _acceptInvite.HandleAsync(
            new AcceptInviteCommand(GetCurrentUserId(), inviteId),
            ct);

        return NoContent();
    }

    [HttpDelete("{clanId:guid}/members/me")]
    public async Task<IActionResult> Leave(Guid clanId, CancellationToken ct)
    {
        await _leaveClan.HandleAsync(
            new LeaveClanCommand(GetCurrentUserId(), clanId),
            ct);

        return NoContent();
    }

    [HttpPost("{clanId:guid}/transfer-ownership")]
    public async Task<IActionResult> TransferOwnership(Guid clanId, TransferOwnershipRequest request, CancellationToken ct)
    {
        var result = await _transferOwnership.HandleAsync(
            new TransferOwnershipCommand(GetCurrentUserId(), clanId, request.NewOwnerId),
            ct);

        return Ok(result);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyClan(CancellationToken ct)
    {
        var result = await _getMyClan.HandleAsync(new GetMyClanQuery(GetCurrentUserId()), ct);
        return Ok(result);
    }

    [HttpGet("invites/pending")]
    public async Task<IActionResult> GetPendingInvites(CancellationToken ct)
    {
        var result = await _getPendingInvites.HandleAsync(new GetPendingInvitesQuery(GetCurrentUserId()), ct);
        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var userId))
            throw new ForbiddenException("Invalid user token.");

        return userId;
    }
}
