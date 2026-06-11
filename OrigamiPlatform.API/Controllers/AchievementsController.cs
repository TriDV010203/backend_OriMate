using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Achievements;
using OrigamiPlatform.Application.DTOs.Achievements;
using OrigamiPlatform.Application.Queries.Achievements;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/achievements")]
public class AchievementsController : ControllerBase
{
    private readonly CreateAchievementHandler _createAchievement;
    private readonly UpdateAchievementHandler _updateAchievement;
    private readonly DeleteAchievementHandler _deleteAchievement;
    private readonly GetUserAchievementsHandler _getUserAchievements;

    public AchievementsController(
        CreateAchievementHandler createAchievement,
        UpdateAchievementHandler updateAchievement,
        DeleteAchievementHandler deleteAchievement,
        GetUserAchievementsHandler getUserAchievements)
        => (_createAchievement, _updateAchievement, _deleteAchievement, _getUserAchievements)
            = (createAchievement, updateAchievement, deleteAchievement, getUserAchievements);

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        CreateAchievementRequest request,
        CancellationToken ct)
    {
        var result = await _createAchievement.HandleAsync(
            new CreateAchievementCommand(GetCurrentUserId(), request),
            ct);

        return CreatedAtAction(
            nameof(GetUserAchievements),
            new { userId = result.UserId },
            result);
    }

    [HttpPut("{achievementId:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(
        Guid achievementId,
        UpdateAchievementRequest request,
        CancellationToken ct)
    {
        var result = await _updateAchievement.HandleAsync(
            new UpdateAchievementCommand(GetCurrentUserId(), achievementId, request),
            ct);

        return Ok(result);
    }

    [HttpDelete("{achievementId:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid achievementId, CancellationToken ct)
    {
        await _deleteAchievement.HandleAsync(
            new DeleteAchievementCommand(GetCurrentUserId(), achievementId),
            ct);

        return NoContent();
    }

    [HttpGet("/api/users/{userId:guid}/achievements")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserAchievements(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var result = await _getUserAchievements.HandleAsync(
            new GetUserAchievementsQuery(userId, GetOptionalCurrentUserId(), page, pageSize),
            ct);

        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyAchievements(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _getUserAchievements.HandleAsync(
            new GetUserAchievementsQuery(userId, userId, page, pageSize),
            ct);

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

    private Guid? GetOptionalCurrentUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
