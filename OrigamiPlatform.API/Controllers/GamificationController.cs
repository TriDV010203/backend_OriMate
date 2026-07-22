using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Queries.Gamification;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/gamification")]
[Authorize]
public class GamificationController : ControllerBase
{
    private readonly GetMySkillLevelHandler _getMySkillLevel;

    public GamificationController(GetMySkillLevelHandler getMySkillLevel)
        => _getMySkillLevel = getMySkillLevel;

    /// <summary>GET /api/gamification/skill-level — current user's SkillPoints and SkillLevel (FT-25).</summary>
    [HttpGet("skill-level")]
    public async Task<IActionResult> GetMySkillLevel(CancellationToken ct)
    {
        var result = await _getMySkillLevel.HandleAsync(new GetMySkillLevelQuery(GetCurrentUserId()), ct);
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
