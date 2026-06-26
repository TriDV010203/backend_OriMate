using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.AdCampaigns;
using OrigamiPlatform.Application.DTOs.AdCampaigns;
using OrigamiPlatform.Application.Queries.AdCampaigns;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/ads")]
public class AdsController : ControllerBase
{
    private readonly RecordImpressionHandler _recordImpression;
    private readonly RecordClickHandler _recordClick;
    private readonly GetServingAdsHandler _getServingAds;

    public AdsController(
        RecordImpressionHandler recordImpression,
        RecordClickHandler recordClick,
        GetServingAdsHandler getServingAds)
        => (_recordImpression, _recordClick, _getServingAds)
            = (recordImpression, recordClick, getServingAds);

    // Returns the banners to display for a placement (live campaigns only).
    [HttpGet("serve")]
    [AllowAnonymous]
    public async Task<IActionResult> Serve([FromQuery] int placementId, CancellationToken ct)
    {
        var result = await _getServingAds.HandleAsync(new GetServingAdsQuery(placementId), ct);
        return Ok(result);
    }

    // AC-01: a page load with an active banner records one impression.
    [HttpPost("impression")]
    [AllowAnonymous]
    public async Task<IActionResult> Impression(TrackAdRequest request, CancellationToken ct)
    {
        var result = await _recordImpression.HandleAsync(
            new RecordImpressionCommand(request.CampaignId, request.BannerId, GetOptionalUserId()), ct);

        return Ok(result);
    }

    // AC-02: a click records one click, deducts the cost, and returns the destination URL.
    [HttpPost("click")]
    [AllowAnonymous]
    public async Task<IActionResult> Click(TrackAdRequest request, CancellationToken ct)
    {
        var result = await _recordClick.HandleAsync(
            new RecordClickCommand(request.CampaignId, request.BannerId, GetOptionalUserId()), ct);

        return Ok(result);
    }

    private Guid? GetOptionalUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var id) ? id : null;
    }
}
