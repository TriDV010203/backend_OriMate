using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;
using OrigamiPlatform.Application.Features.AdminConfiguration.Services;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminConfigService _admin;

    public AdminController(IAdminConfigService admin) => _admin = admin;

    // ── CATEGORIES ──────────────────────────────────────────────────────

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var result = await _admin.GetCategoriesAsync(ct);
        return Ok(result);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(CreateCategoryRequest req, CancellationToken ct)
    {
        var result = await _admin.CreateCategoryAsync(GetCurrentUserId(), req, ct);
        return Ok(result);
    }

    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, UpdateCategoryRequest req, CancellationToken ct)
    {
        var result = await _admin.UpdateCategoryAsync(GetCurrentUserId(), id, req, ct);
        return Ok(result);
    }

    // ── BLOCKED WORDS ───────────────────────────────────────────────────

    [HttpGet("blocked-words")]
    public async Task<IActionResult> GetBlockedWords(CancellationToken ct)
    {
        var result = await _admin.GetBlockedWordsAsync(ct);
        return Ok(result);
    }

    [HttpPost("blocked-words")]
    public async Task<IActionResult> AddBlockedWord(CreateBlockedWordRequest req, CancellationToken ct)
    {
        var result = await _admin.AddBlockedWordAsync(GetCurrentUserId(), req, ct);
        return Ok(result);
    }

    [HttpDelete("blocked-words/{id:int}")]
    public async Task<IActionResult> RemoveBlockedWord(int id, CancellationToken ct)
    {
        await _admin.RemoveBlockedWordAsync(GetCurrentUserId(), id, ct);
        return Ok(new { message = "Blocked word removed successfully." });
    }

    // ── USER MANAGEMENT ─────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? keyword,
        [FromQuery] string? status,
        [FromQuery] string? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _admin.GetUsersAsync(keyword, status, role, page, pageSize, ct);
        return Ok(result);
    }

    [HttpPut("users/{id:guid}/assign-role")]
    public async Task<IActionResult> AssignRole(Guid id, AssignRoleRequest req, CancellationToken ct)
    {
        await _admin.AssignRoleAsync(GetCurrentUserId(), id, req, ct);
        return Ok(new { message = "Role assigned successfully." });
    }

    [HttpDelete("users/{id:guid}/remove-role")]
    public async Task<IActionResult> RemoveRole(Guid id, RemoveRoleRequest req, CancellationToken ct)
    {
        await _admin.RemoveRoleAsync(GetCurrentUserId(), id, req, ct);
        return Ok(new { message = "Role removed successfully." });
    }

    [HttpPut("users/{id:guid}/suspend")]
    public async Task<IActionResult> SuspendUser(Guid id, SuspendUserRequest req, CancellationToken ct)
    {
        await _admin.SuspendUserAsync(GetCurrentUserId(), id, req, ct);
        return Ok(new { message = "User account suspended." });
    }

    [HttpPut("users/{id:guid}/activate")]
    public async Task<IActionResult> ActivateUser(Guid id, CancellationToken ct)
    {
        await _admin.ActivateUserAsync(GetCurrentUserId(), id, ct);
        return Ok(new { message = "User account activated." });
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new ForbiddenException("User identity not found in token.");
        return Guid.Parse(sub);
    }
}
