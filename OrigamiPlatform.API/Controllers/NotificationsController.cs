using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.Notifications;
using OrigamiPlatform.Infrastructure.Persistence;

namespace OrigamiPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public NotificationsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var query = _db.Notifications.Where(n => n.RecipientId == userId);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(n => new NotificationDto(n.Id, n.Type, n.Message, n.EntityType, n.EntityId, n.IsRead, n.CreatedAt))
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Ok(new PagedResult<NotificationDto>(items, total, page, pageSize, totalPages));
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var notif = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.RecipientId == GetCurrentUserId(), ct);
        if (notif == null) return NotFound();

        notif.IsRead = true;
        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "Marked as read." });
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var unread = await _db.Notifications.Where(n => n.RecipientId == GetCurrentUserId() && !n.IsRead).ToListAsync(ct);
        unread.ForEach(n => n.IsRead = true);
        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "All marked as read." });
    }

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!);
}