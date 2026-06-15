using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Follows;

namespace OrigamiPlatform.API.Controllers;

[Authorize] // Chặn Guest (NAC-04)
[ApiController]
[Route("api/users")] // Dùng route api/users để API đẹp hơn: api/users/{id}/toggle-follow
public class FollowsController : ControllerBase
{
    [HttpPost("{id}/toggle-follow")]
    public async Task<IActionResult> ToggleFollow(
        [FromRoute] Guid id, // ID của người mà mình muốn theo dõi
        [FromServices] ToggleFollowHandler handler,
        CancellationToken ct)
    {
        var followerId = GetCurrentUserId();
        var cmd = new ToggleFollowCommand(followerId, id);

        var isFollowing = await handler.HandleAsync(cmd, ct);

        var message = isFollowing ? "Successfully followed the user." : "Successfully unfollowed the user.";
        return Ok(new { message, isFollowing });
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(value))
            throw new UnauthorizedAccessException();

        return Guid.Parse(value);
    }
}