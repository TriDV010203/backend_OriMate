using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Gamification;
using OrigamiPlatform.Application.Queries.Gamification;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/gamification")]
[Authorize]
public class GamificationController : ControllerBase
{
    private readonly GetMySkillLevelHandler _getMySkillLevel;
    private readonly GetMyStreakHandler _getMyStreak;
    private readonly GetMyHatGapBalanceHandler _getMyHatGapBalance;
    private readonly GetMyHatGapLevelHandler _getMyHatGapLevel;
    private readonly PurchaseStreakFreezeHandler _purchaseStreakFreeze;
    private readonly GetBadgeCatalogHandler _getBadgeCatalog;
    private readonly GetMyBadgesHandler _getMyBadges;

    public GamificationController(
        GetMySkillLevelHandler getMySkillLevel,
        GetMyStreakHandler getMyStreak,
        GetMyHatGapBalanceHandler getMyHatGapBalance,
        GetMyHatGapLevelHandler getMyHatGapLevel,
        PurchaseStreakFreezeHandler purchaseStreakFreeze,
        GetBadgeCatalogHandler getBadgeCatalog,
        GetMyBadgesHandler getMyBadges)
        => (_getMySkillLevel, _getMyStreak, _getMyHatGapBalance, _getMyHatGapLevel, _purchaseStreakFreeze, _getBadgeCatalog, _getMyBadges)
            = (getMySkillLevel, getMyStreak, getMyHatGapBalance, getMyHatGapLevel, purchaseStreakFreeze, getBadgeCatalog, getMyBadges);

    /// <summary>GET /api/gamification/skill-level — current user's SkillPoints and SkillLevel (FT-25).</summary>
    [HttpGet("skill-level")]
    public async Task<IActionResult> GetMySkillLevel(CancellationToken ct)
    {
        var result = await _getMySkillLevel.HandleAsync(new GetMySkillLevelQuery(GetCurrentUserId()), ct);
        return Ok(result);
    }

    /// <summary>GET /api/gamification/streak — current user's Daily Streak (FT-26).</summary>
    [HttpGet("streak")]
    public async Task<IActionResult> GetMyStreak(CancellationToken ct)
    {
        var result = await _getMyStreak.HandleAsync(new GetMyStreakQuery(GetCurrentUserId()), ct);
        return Ok(result);
    }

    /// <summary>GET /api/gamification/hatgap-balance — current user's Hạt Gấp balance (FT-28).</summary>
    [HttpGet("hatgap-balance")]
    public async Task<IActionResult> GetMyHatGapBalance(CancellationToken ct)
    {
        var result = await _getMyHatGapBalance.HandleAsync(new GetMyHatGapBalanceQuery(GetCurrentUserId()), ct);
        return Ok(result);
    }

    /// <summary>GET /api/gamification/level — current user's Hạt Gấp level and progress to the next level.</summary>
    [HttpGet("level")]
    public async Task<IActionResult> GetMyLevel(CancellationToken ct)
    {
        var result = await _getMyHatGapLevel.HandleAsync(new GetMyHatGapLevelQuery(GetCurrentUserId()), ct);
        return Ok(result);
    }

    /// <summary>POST /api/gamification/streak-freeze — spend Hạt Gấp to buy a Streak Freeze (FT-26/FT-28).</summary>
    [HttpPost("streak-freeze")]
    public async Task<IActionResult> PurchaseStreakFreeze(CancellationToken ct)
    {
        var result = await _purchaseStreakFreeze.HandleAsync(new PurchaseStreakFreezeCommand(GetCurrentUserId()), ct);
        return Ok(result);
    }

    /// <summary>GET /api/gamification/badges — full badge catalog (FT-35).</summary>
    [HttpGet("badges")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBadgeCatalog(CancellationToken ct)
    {
        var result = await _getBadgeCatalog.HandleAsync(new GetBadgeCatalogQuery(), ct);
        return Ok(result);
    }

    /// <summary>GET /api/gamification/me/badges — current user's earned badges (FT-35).</summary>
    [HttpGet("me/badges")]
    public async Task<IActionResult> GetMyBadges(CancellationToken ct)
    {
        var result = await _getMyBadges.HandleAsync(new GetMyBadgesQuery(GetCurrentUserId()), ct);
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
