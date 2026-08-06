using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Users;
using OrigamiPlatform.Application.DTOs.Users;
using OrigamiPlatform.Application.Queries.Users;
using System.Security.Claims;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}/profile")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProfile(
        [FromRoute] Guid id,
        [FromServices] GetCreatorProfileHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetCreatorProfileQuery(id, GetCurrentUserId()), ct);

        return Ok(result);
    }

    /// <summary>GET /api/users/top-creators — Nhà sáng tạo nổi bật, xếp hạng theo số lượng người theo dõi.</summary>
    [HttpGet("top-creators")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTopCreators(
        [FromServices] GetFeaturedCreatorsHandler handler,
        CancellationToken ct,
        [FromQuery] int count = 4)
    {
        var result = await handler.HandleAsync(new GetFeaturedCreatorsQuery(count, GetCurrentUserId()), ct);

        return Ok(result);
    }

    [HttpGet("{id}/followers")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFollowers(
        [FromRoute] Guid id,
        [FromServices] GetFollowersHandler handler,
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await handler.HandleAsync(new GetFollowersQuery(id, GetCurrentUserId(), page, pageSize), ct);

        return Ok(result);
    }

    [HttpGet("{id}/following")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFollowing(
        [FromRoute] Guid id,
        [FromServices] GetFollowingHandler handler,
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await handler.HandleAsync(new GetFollowingQuery(id, GetCurrentUserId(), page, pageSize), ct);

        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(userIdClaim, out var parsedId) ? parsedId : null;
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        [FromServices] UpdateProfileHandler handler,
        CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("Token không hợp lệ.");
        }

        var command = new UpdateProfileCommand(userId, request.DisplayName, request.AvatarUrl, request.Bio);
        await handler.HandleAsync(command, ct);

        return Ok(new { message = "Cập nhật Profile thành công!" });
    }

    /// <summary>GET /api/users/me/onboarding-status — FT-29: whether the current user has completed first-run onboarding.</summary>
    [HttpGet("me/onboarding-status")]
    [Authorize]
    public async Task<IActionResult> GetOnboardingStatus(
        [FromServices] GetOnboardingStatusHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(new GetOnboardingStatusQuery(GetCurrentUserId()!.Value), ct);
        return Ok(result);
    }

    /// <summary>POST /api/users/me/complete-onboarding — FT-29: marks first-run onboarding as completed.</summary>
    [HttpPost("me/complete-onboarding")]
    [Authorize]
    public async Task<IActionResult> CompleteOnboarding(
        [FromServices] CompleteOnboardingHandler handler,
        CancellationToken ct)
    {
        await handler.HandleAsync(new CompleteOnboardingCommand(GetCurrentUserId()!.Value), ct);
        return Ok(new { message = "Onboarding completed." });
    }
}