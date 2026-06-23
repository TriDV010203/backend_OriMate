using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.AdCampaigns;
using OrigamiPlatform.Application.DTOs.AdCampaigns;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/ads")]
public class AdsController : ControllerBase
{
    private readonly RecordImpressionHandler _recordImpression;
    private readonly RecordClickHandler _recordClick;

    public AdsController(RecordImpressionHandler recordImpression, RecordClickHandler recordClick)
        => (_recordImpression, _recordClick) = (recordImpression, recordClick);

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
