using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Likes;
using OrigamiPlatform.Application.DTOs.Likes;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/likes")]
[Authorize]
public class LikesController : ControllerBase
{
    private readonly ToggleLikeHandler _toggleLike;

    public LikesController(ToggleLikeHandler toggleLike)
        => _toggleLike = toggleLike;

    [HttpPost("toggle")]
    public async Task<IActionResult> ToggleLike([FromBody] ToggleLikeRequest request, CancellationToken ct)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out Guid userId))
            return Unauthorized();

        var command = new ToggleLikeCommand(userId, request.TargetId, request.TargetType);

        // Trả về true (Đã like) hoặc false (Đã unlike)
        var isLiked = await _toggleLike.HandleAsync(command, ct);

        return Ok(new { IsLiked = isLiked });
    }
}