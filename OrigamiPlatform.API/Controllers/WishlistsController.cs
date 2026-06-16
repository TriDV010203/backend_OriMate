using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Wishlists;
using OrigamiPlatform.Application.DTOs.Wishlists;
using OrigamiPlatform.Application.Queries.Wishlists;

namespace OrigamiPlatform.API.Controllers;

[Authorize] // Chặn Khách vãng lai, phải có Token mới được xài (NAC-04)
[ApiController]
[Route("api/[controller]")]
public class WishlistsController : ControllerBase
{
    [HttpPost("toggle")]
    public async Task<IActionResult> ToggleWishlist(
        [FromBody] ToggleWishlistRequest request,
        [FromServices] ToggleWishlistHandler handler,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var cmd = new ToggleWishlistCommand(userId, request.TargetId, request.TargetType);

        // Gọi Handler, nhận về true (Saved) hoặc false (Unsaved)
        var isSaved = await handler.HandleAsync(cmd, ct);

        var message = isSaved ? "Item saved to wishlist." : "Item removed from wishlist.";
        return Ok(new { message, isSaved });
    }

    [HttpGet("my-wishlist")]
    public async Task<IActionResult> GetMyWishlist(
        [FromServices] GetWishlistHandler handler,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var query = new GetWishlistQuery(userId, page, pageSize);
        var result = await handler.HandleAsync(query, ct);

        return Ok(result);
    }

    // Hàm lấy ID từ Token
    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(value))
            throw new UnauthorizedAccessException();

        return Guid.Parse(value);
    }
}