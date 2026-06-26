using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.AdCampaigns;
using OrigamiPlatform.Application.DTOs.AdCampaigns;
using OrigamiPlatform.Application.Queries.AdCampaigns;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/ad-campaigns")]
[Authorize]
public class AdCampaignsController : ControllerBase
{
    private readonly CreateAdCampaignHandler _createCampaign;
    private readonly ReviewAdCampaignHandler _reviewCampaign;
    private readonly GetMyCampaignsHandler _getMyCampaigns;
    private readonly GetPendingCampaignsHandler _getPendingCampaigns;
    private readonly GetAdCampaignHandler _getCampaign;
    private readonly GetCampaignDashboardHandler _getDashboard;
    private readonly GetAdOverviewHandler _getOverview;

    public AdCampaignsController(
        CreateAdCampaignHandler createCampaign,
        ReviewAdCampaignHandler reviewCampaign,
        GetMyCampaignsHandler getMyCampaigns,
        GetPendingCampaignsHandler getPendingCampaigns,
        GetAdCampaignHandler getCampaign,
        GetCampaignDashboardHandler getDashboard,
        GetAdOverviewHandler getOverview)
        => (_createCampaign, _reviewCampaign, _getMyCampaigns, _getPendingCampaigns, _getCampaign, _getDashboard, _getOverview)
            = (createCampaign, reviewCampaign, getMyCampaigns, getPendingCampaigns, getCampaign, getDashboard, getOverview);

    // UC-41 / NAC-01: only Advertising Partners can create campaigns.
    [HttpPost]
    [Authorize(Roles = "AdvertisingPartner")]
    public async Task<IActionResult> Create(CreateAdCampaignRequest request, CancellationToken ct)
    {
        var result = await _createCampaign.HandleAsync(
            new CreateAdCampaignCommand(GetCurrentUserId(), request), ct);

        return Created($"/api/ad-campaigns/{result.Id}", result);
    }

    // UC-42: only Managers can approve or reject campaigns.
    [HttpPost("{campaignId:guid}/review")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Review(
        Guid campaignId,
        ReviewAdCampaignRequest request,
        CancellationToken ct)
    {
        var result = await _reviewCampaign.HandleAsync(
            new ReviewAdCampaignCommand(campaignId, GetCurrentUserId(), request.Approve, request.Reason), ct);

        return Ok(result);
    }

    // Partner views their own campaigns.
    [HttpGet("mine")]
    [Authorize(Roles = "AdvertisingPartner")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 12, CancellationToken ct = default)
    {
        var result = await _getMyCampaigns.HandleAsync(
            new GetMyCampaignsQuery(GetCurrentUserId(), page, pageSize), ct);
        return Ok(result);
    }

    // Manager views the pending-approval queue.
    [HttpGet("pending")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> GetPending(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 12, CancellationToken ct = default)
    {
        var result = await _getPendingCampaigns.HandleAsync(
            new GetPendingCampaignsQuery(page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{campaignId:guid}")]
    public async Task<IActionResult> GetById(Guid campaignId, CancellationToken ct)
    {
        var isPrivileged = User.IsInRole("Manager") || User.IsInRole("Admin");
        var result = await _getCampaign.HandleAsync(
            new GetAdCampaignQuery(campaignId, GetCurrentUserId(), isPrivileged), ct);
        return Ok(result);
    }

    // UC-43 / AC-04: campaign performance dashboard (partner own, or Manager/Admin).
    [HttpGet("{campaignId:guid}/dashboard")]
    public async Task<IActionResult> Dashboard(Guid campaignId, CancellationToken ct)
    {
        var isPrivileged = User.IsInRole("Manager") || User.IsInRole("Admin");
        var result = await _getDashboard.HandleAsync(
            new GetCampaignDashboardQuery(campaignId, GetCurrentUserId(), isPrivileged), ct);
        return Ok(result);
    }

    // Platform-wide advertising overview for Admin/Manager.
    [HttpGet("dashboard")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Overview(CancellationToken ct)
    {
        var result = await _getOverview.HandleAsync(new GetAdOverviewQuery(), ct);
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
