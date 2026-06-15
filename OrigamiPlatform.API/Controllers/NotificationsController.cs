using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Notifications;
using OrigamiPlatform.Application.Queries.Notifications;

namespace OrigamiPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    public NotificationsController() { }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromServices] GetNotificationsHandler handler,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await handler.HandleAsync(new GetNotificationsQuery(userId, page, pageSize), ct);
        return Ok(result);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(
        [FromRoute] Guid id,
        [FromServices] MarkNotificationAsReadHandler handler,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        await handler.HandleAsync(new MarkNotificationAsReadCommand(id, userId), ct);
        return Ok(new { message = "Marked as read." });
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(
        [FromServices] MarkAllNotificationsAsReadHandler handler,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        await handler.HandleAsync(new MarkAllNotificationsAsReadCommand(userId), ct);
        return Ok(new { message = "All marked as read." });
    }

    private Guid GetCurrentUserId() => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!
    );
}