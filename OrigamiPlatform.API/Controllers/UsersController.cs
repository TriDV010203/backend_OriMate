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
}