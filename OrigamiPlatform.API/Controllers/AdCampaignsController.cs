using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.AdCampaigns;
using OrigamiPlatform.Application.DTOs.AdCampaigns;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/ad-campaigns")]
[Authorize]
public class AdCampaignsController : ControllerBase
{
    private readonly CreateAdCampaignHandler _createCampaign;

    public AdCampaignsController(CreateAdCampaignHandler createCampaign)
        => _createCampaign = createCampaign;

    // UC-41 / NAC-01: only Advertising Partners can create campaigns.
    [HttpPost]
    [Authorize(Roles = "AdvertisingPartner")]
    public async Task<IActionResult> Create(CreateAdCampaignRequest request, CancellationToken ct)
    {
        var result = await _createCampaign.HandleAsync(
            new CreateAdCampaignCommand(GetCurrentUserId(), request), ct);

        return Created($"/api/ad-campaigns/{result.Id}", result);
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
