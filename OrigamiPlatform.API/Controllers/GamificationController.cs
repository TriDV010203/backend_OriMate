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
    private readonly GetMyQuestProgressHandler _getMyQuestProgress;
    private readonly GetMyHatGapBalanceHandler _getMyHatGapBalance;
    private readonly GetMyHatGapLevelHandler _getMyHatGapLevel;
    private readonly PurchaseStreakFreezeHandler _purchaseStreakFreeze;

    public GamificationController(
        GetMySkillLevelHandler getMySkillLevel,
        GetMyStreakHandler getMyStreak,
        GetMyQuestProgressHandler getMyQuestProgress,
        GetMyHatGapBalanceHandler getMyHatGapBalance,
        GetMyHatGapLevelHandler getMyHatGapLevel,
        PurchaseStreakFreezeHandler purchaseStreakFreeze)
        => (_getMySkillLevel, _getMyStreak, _getMyQuestProgress, _getMyHatGapBalance, _getMyHatGapLevel, _purchaseStreakFreeze)
            = (getMySkillLevel, getMyStreak, getMyQuestProgress, getMyHatGapBalance, getMyHatGapLevel, purchaseStreakFreeze);

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

    /// <summary>GET /api/gamification/quest-today — current user's Daily Quest progress (FT-27).</summary>
    [HttpGet("quest-today")]
    public async Task<IActionResult> GetMyQuestToday(CancellationToken ct)
    {
        var result = await _getMyQuestProgress.HandleAsync(new GetMyQuestProgressQuery(GetCurrentUserId()), ct);
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

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var userId))
            throw new ForbiddenException("Invalid user token.");

        return userId;
    }
}
