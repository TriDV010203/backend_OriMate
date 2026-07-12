using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.DTOs;
using OrigamiPlatform.Application.Features.Tutorials.DTOs;
using OrigamiPlatform.Application.Queries.Tutorials;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/tutorials")]
public class TutorialsController : ControllerBase
{
    private readonly GetTutorialsHandler _getTutorials;
    private readonly GetTutorialBySlugHandler _getTutorialBySlug;
    private readonly GetMyTutorialsHandler _getMyTutorials;
    private readonly CreateTutorialHandler _createTutorial;
    private readonly SubmitTutorialHandler _submitTutorial;
    private readonly ManagerPublishHandler _managerPublish;
    private readonly ManagerRejectHandler _managerReject;
    private readonly ManagerRemoveHandler _managerRemove;
    private readonly CreateWorkingCopyHandler _createWorkingCopy;
    private readonly UpdateWorkingCopyHandler _updateWorkingCopy;
    private readonly SubmitEditHandler _submitEdit;
    private readonly ManagerApproveEditHandler _managerApproveEdit;
    private readonly ManagerRejectEditHandler _managerRejectEdit;

    public TutorialsController(
        GetTutorialsHandler getTutorials,
        GetTutorialBySlugHandler getTutorialBySlug,
        GetMyTutorialsHandler getMyTutorials,
        CreateTutorialHandler createTutorial,
        SubmitTutorialHandler submitTutorial,
        ManagerPublishHandler managerPublish,
        ManagerRejectHandler managerReject,
        ManagerRemoveHandler managerRemove,
        CreateWorkingCopyHandler createWorkingCopy,
        UpdateWorkingCopyHandler updateWorkingCopy,
        SubmitEditHandler submitEdit,
        ManagerApproveEditHandler managerApproveEdit,
        ManagerRejectEditHandler managerRejectEdit)
    {
        _getTutorials = getTutorials;
        _getTutorialBySlug = getTutorialBySlug;
        _getMyTutorials = getMyTutorials;
        _createTutorial = createTutorial;
        _submitTutorial = submitTutorial;
        _managerPublish = managerPublish;
        _managerReject = managerReject;
        _managerRemove = managerRemove;
        _createWorkingCopy = createWorkingCopy;
        _updateWorkingCopy = updateWorkingCopy;
        _submitEdit = submitEdit;
        _managerApproveEdit = managerApproveEdit;
        _managerRejectEdit = managerRejectEdit;
    }

    // ── Public ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetTutorials(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] string? difficulty,
        [FromQuery] string? type,
        [FromQuery] string? sortBy,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _getTutorials.HandleAsync(
            new GetTutorialsQuery(search, categoryId, difficulty, type, sortBy, page, pageSize, GetCurrentUserId()), ct);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBySlug([FromRoute] string slug, CancellationToken ct)
    {
        var result = await _getTutorialBySlug.HandleAsync(
            new GetTutorialBySlugQuery(slug, GetCurrentUserId()), ct);
        return Ok(result);
    }

    // ── Authoring ────────────────────────────────────────────────────────────

    /// <summary>POST /api/tutorials — Create a draft tutorial (any authenticated user).</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateTutorial(
        [FromBody] CreateTutorialRequest request, CancellationToken ct)
    {
        var authorId = GetCurrentUserId()!.Value;
        var result = await _createTutorial.HandleAsync(new CreateTutorialCommand(authorId, request), ct);
        return CreatedAtAction(nameof(GetBySlug), new { slug = result.Slug }, result);
    }

    /// <summary>PUT /api/tutorials/{id}/submit — Submit draft for manager review (author only, BR-TUT-01).</summary>
    [HttpPut("{id:guid}/submit")]
    [Authorize]
    public async Task<IActionResult> SubmitForReview(Guid id, CancellationToken ct)
    {
        var authorId = GetCurrentUserId()!.Value;
        var result = await _submitTutorial.HandleAsync(new SubmitTutorialCommand(id, authorId), ct);
        return Ok(result);
    }

    /// <summary>GET /api/tutorials/my-tutorials — Author's own tutorials across all statuses.</summary>
    [HttpGet("my-tutorials")]
    [Authorize]
    public async Task<IActionResult> GetMyTutorials(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var authorId = GetCurrentUserId()!.Value;
        var result = await _getMyTutorials.HandleAsync(new GetMyTutorialsQuery(authorId, page, pageSize), ct);
        return Ok(result);
    }

    // ── Edit-after-publish ───────────────────────────────────────────────────

    /// <summary>POST /api/tutorials/{id}/edit — Author creates a working copy of a published tutorial.</summary>
    [HttpPost("{id:guid}/edit")]
    [Authorize]
    public async Task<IActionResult> CreateWorkingCopy(Guid id, CancellationToken ct)
    {
        var authorId = GetCurrentUserId()!.Value;
        var result = await _createWorkingCopy.HandleAsync(new CreateWorkingCopyCommand(id, authorId), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/tutorials/{id}/edit-content — Author updates the working copy's content.</summary>
    [HttpPut("{id:guid}/edit-content")]
    [Authorize]
    public async Task<IActionResult> UpdateWorkingCopy(
        Guid id, [FromBody] UpdateTutorialRequest request, CancellationToken ct)
    {
        var authorId = GetCurrentUserId()!.Value;
        var result = await _updateWorkingCopy.HandleAsync(new UpdateWorkingCopyCommand(id, authorId, request), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/tutorials/{id}/submit-edit — Author submits working copy for manager review (BR-TUT-01).</summary>
    [HttpPut("{id:guid}/submit-edit")]
    [Authorize]
    public async Task<IActionResult> SubmitEdit(Guid id, CancellationToken ct)
    {
        var authorId = GetCurrentUserId()!.Value;
        var result = await _submitEdit.HandleAsync(new SubmitEditCommand(id, authorId), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/tutorials/{id}/approve-edit — Manager approves edit: swaps content into original.</summary>
    [HttpPut("{id:guid}/approve-edit")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ManagerApproveEdit(Guid id, CancellationToken ct)
    {
        var managerId = GetCurrentUserId()!.Value;
        await _managerApproveEdit.HandleAsync(new ManagerApproveEditCommand(id, managerId), ct);
        return Ok(new MessageResponse("Tutorial edit approved and published."));
    }

    /// <summary>PUT /api/tutorials/{id}/reject-edit — Manager rejects edit, returns working copy to author.</summary>
    [HttpPut("{id:guid}/reject-edit")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ManagerRejectEdit(
        Guid id, [FromBody] ManagerRejectRequest request, CancellationToken ct)
    {
        var managerId = GetCurrentUserId()!.Value;
        await _managerRejectEdit.HandleAsync(new ManagerRejectEditCommand(id, managerId, request), ct);
        return Ok(new MessageResponse("Tutorial edit rejected. Author has been notified."));
    }

    // ── Manager final approval ───────────────────────────────────────────────

    /// <summary>PUT /api/tutorials/{id}/publish — Manager publishes a tutorial (BR-16).</summary>
    [HttpPut("{id:guid}/publish")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ManagerPublish(Guid id, CancellationToken ct)
    {
        var managerId = GetCurrentUserId()!.Value;
        await _managerPublish.HandleAsync(new ManagerPublishCommand(id, managerId), ct);
        return Ok(new MessageResponse("Tutorial has been published successfully."));
    }

    /// <summary>PUT /api/tutorials/{id}/reject — Manager sends a tutorial back for revision (BR-TUT-01, BR-18: not terminal).</summary>
    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ManagerReject(
        Guid id, [FromBody] ManagerRejectRequest request, CancellationToken ct)
    {
        var managerId = GetCurrentUserId()!.Value;
        await _managerReject.HandleAsync(new ManagerRejectCommand(id, managerId, request), ct);
        return Ok(new MessageResponse("Tutorial has been sent back for revision."));
    }

    /// <summary>DELETE /api/tutorials/{id} — Manager soft-removes a published tutorial (BR-16, terminal).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ManagerRemove(
        Guid id, [FromBody] ManagerRemoveRequest? request, CancellationToken ct)
    {
        var managerId = GetCurrentUserId()!.Value;
        await _managerRemove.HandleAsync(new ManagerRemoveCommand(id, managerId, request), ct);
        return Ok(new MessageResponse("Tutorial has been removed."));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return value is null ? null : Guid.Parse(value);
    }
}
