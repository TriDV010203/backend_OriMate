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
        Guid? currentUserId = null;

        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (Guid.TryParse(userIdClaim, out var parsedId))
            {
                currentUserId = parsedId;
            }
        }

        var result = await handler.HandleAsync(new GetCreatorProfileQuery(id, currentUserId), ct);

        return Ok(result);
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