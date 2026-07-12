using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.AdminConfiguration;
using OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;
using OrigamiPlatform.Application.Queries.AdminConfiguration;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly GetCategoriesHandler _getCategories;
    private readonly CreateCategoryHandler _createCategory;
    private readonly UpdateCategoryHandler _updateCategory;
    private readonly GetBlockedWordsHandler _getBlockedWords;
    private readonly CreateBlockedWordHandler _createBlockedWord;
    private readonly RemoveBlockedWordHandler _removeBlockedWord;
    private readonly GetUsersHandler _getUsers;
    private readonly AssignRoleHandler _assignRole;
    private readonly RemoveRoleHandler _removeRole;
    private readonly SuspendUserHandler _suspendUser;
    private readonly ActivateUserHandler _activateUser;

    public AdminController(
        GetCategoriesHandler getCategories,
        CreateCategoryHandler createCategory,
        UpdateCategoryHandler updateCategory,
        GetBlockedWordsHandler getBlockedWords,
        CreateBlockedWordHandler createBlockedWord,
        RemoveBlockedWordHandler removeBlockedWord,
        GetUsersHandler getUsers,
        AssignRoleHandler assignRole,
        RemoveRoleHandler removeRole,
        SuspendUserHandler suspendUser,
        ActivateUserHandler activateUser)
    {
        _getCategories = getCategories;
        _createCategory = createCategory;
        _updateCategory = updateCategory;
        _getBlockedWords = getBlockedWords;
        _createBlockedWord = createBlockedWord;
        _removeBlockedWord = removeBlockedWord;
        _getUsers = getUsers;
        _assignRole = assignRole;
        _removeRole = removeRole;
        _suspendUser = suspendUser;
        _activateUser = activateUser;
    }

    // ── CATEGORIES ──────────────────────────────────────────────────────

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var result = await _getCategories.HandleAsync(new GetCategoriesQuery(), ct);
        return Ok(result);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(CreateCategoryRequest req, CancellationToken ct)
    {
        var result = await _createCategory.HandleAsync(new CreateCategoryCommand(GetCurrentUserId(), req), ct);
        return Ok(result);
    }

    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, UpdateCategoryRequest req, CancellationToken ct)
    {
        var result = await _updateCategory.HandleAsync(new UpdateCategoryCommand(GetCurrentUserId(), id, req), ct);
        return Ok(result);
    }

    // ── BLOCKED WORDS ───────────────────────────────────────────────────

    [HttpGet("blocked-words")]
    public async Task<IActionResult> GetBlockedWords(CancellationToken ct)
    {
        var result = await _getBlockedWords.HandleAsync(new GetBlockedWordsQuery(), ct);
        return Ok(result);
    }

    [HttpPost("blocked-words")]
    public async Task<IActionResult> AddBlockedWord(CreateBlockedWordRequest req, CancellationToken ct)
    {
        var result = await _createBlockedWord.HandleAsync(new CreateBlockedWordCommand(GetCurrentUserId(), req), ct);
        return Ok(result);
    }

    [HttpDelete("blocked-words/{id:int}")]
    public async Task<IActionResult> RemoveBlockedWord(int id, CancellationToken ct)
    {
        await _removeBlockedWord.HandleAsync(new RemoveBlockedWordCommand(GetCurrentUserId(), id), ct);
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
        var result = await _getUsers.HandleAsync(new GetUsersQuery(keyword, status, role, page, pageSize), ct);
        return Ok(result);
    }

    [HttpPut("users/{id:guid}/assign-role")]
    public async Task<IActionResult> AssignRole(Guid id, AssignRoleRequest req, CancellationToken ct)
    {
        await _assignRole.HandleAsync(new AssignRoleCommand(GetCurrentUserId(), id, req), ct);
        return Ok(new { message = "Role assigned successfully." });
    }

    [HttpDelete("users/{id:guid}/remove-role")]
    public async Task<IActionResult> RemoveRole(Guid id, RemoveRoleRequest req, CancellationToken ct)
    {
        await _removeRole.HandleAsync(new RemoveRoleCommand(GetCurrentUserId(), id, req), ct);
        return Ok(new { message = "Role removed successfully." });
    }

    [HttpPut("users/{id:guid}/suspend")]
    public async Task<IActionResult> SuspendUser(Guid id, SuspendUserRequest req, CancellationToken ct)
    {
        await _suspendUser.HandleAsync(new SuspendUserCommand(GetCurrentUserId(), id, req), ct);
        return Ok(new { message = "User account suspended." });
    }

    [HttpPut("users/{id:guid}/activate")]
    public async Task<IActionResult> ActivateUser(Guid id, CancellationToken ct)
    {
        await _activateUser.HandleAsync(new ActivateUserCommand(GetCurrentUserId(), id), ct);
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
