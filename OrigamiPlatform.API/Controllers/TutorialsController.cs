using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.DTOs;
using OrigamiPlatform.Application.Features.Tutorials.DTOs;
using OrigamiPlatform.Application.Features.Tutorials.Services;
using OrigamiPlatform.Application.Queries.Tutorials;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/tutorials")]
public class TutorialsController : ControllerBase
{
    private readonly GetTutorialsHandler _getTutorials;
    private readonly GetTutorialBySlugHandler _getTutorialBySlug;
    private readonly ITutorialService _tutorialService;

    public TutorialsController(
        GetTutorialsHandler getTutorials,
        GetTutorialBySlugHandler getTutorialBySlug,
        ITutorialService tutorialService)
    {
        _getTutorials = getTutorials;
        _getTutorialBySlug = getTutorialBySlug;
        _tutorialService = tutorialService;
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
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
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
        var result = await _tutorialService.CreateDraftAsync(request, authorId, ct);
        return CreatedAtAction(nameof(GetBySlug), new { slug = result.Slug }, result);
    }

    /// <summary>PUT /api/tutorials/{id}/submit — Submit draft for contributor review (author only).</summary>
    [HttpPut("{id:guid}/submit")]
    [Authorize]
    public async Task<IActionResult> SubmitForReview(Guid id, CancellationToken ct)
    {
        var authorId = GetCurrentUserId()!.Value;
        var result = await _tutorialService.SubmitForReviewAsync(id, authorId, ct);
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
        var result = await _tutorialService.GetMyTutorialsAsync(authorId, page, pageSize, ct);
        return Ok(result);
    }

    // ── Contributor review ───────────────────────────────────────────────────

    /// <summary>GET /api/tutorials/contributor-queue — FIFO queue of tutorials awaiting contributor review.</summary>
    [HttpGet("contributor-queue")]
    [Authorize(Roles = "ContributorReviewer")]
    public async Task<IActionResult> GetContributorQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var result = await _tutorialService.GetContributorQueueAsync(page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>PUT /api/tutorials/{id}/contributor-approve — Contributor approves, escalates to manager.</summary>
    [HttpPut("{id:guid}/contributor-approve")]
    [Authorize(Roles = "ContributorReviewer")]
    public async Task<IActionResult> ContributorApprove(Guid id, CancellationToken ct)
    {
        var reviewerId = GetCurrentUserId()!.Value;
        await _tutorialService.ContributorApproveAsync(id, reviewerId, ct);
        return Ok(new MessageResponse("Tutorial approved and sent to manager review."));
    }

    /// <summary>PUT /api/tutorials/{id}/contributor-request-revision — Contributor requests revision from author.</summary>
    [HttpPut("{id:guid}/contributor-request-revision")]
    [Authorize(Roles = "ContributorReviewer")]
    public async Task<IActionResult> ContributorRequestRevision(
        Guid id, [FromBody] ReviewActionRequest request, CancellationToken ct)
    {
        var reviewerId = GetCurrentUserId()!.Value;
        await _tutorialService.ContributorRequestRevisionAsync(id, reviewerId, request.Reason, ct);
        return Ok(new MessageResponse("Revision request sent to the tutorial author."));
    }

    // ── Edit-after-publish ───────────────────────────────────────────────────

    /// <summary>POST /api/tutorials/{id}/edit — Author creates a working copy of a published tutorial.</summary>
    [HttpPost("{id:guid}/edit")]
    [Authorize]
    public async Task<IActionResult> CreateWorkingCopy(Guid id, CancellationToken ct)
    {
        var authorId = GetCurrentUserId()!.Value;
        var result = await _tutorialService.CreateWorkingCopyAsync(id, authorId, ct);
        return Ok(result);
    }

    /// <summary>PUT /api/tutorials/{id}/edit-content — Author updates the working copy's content.</summary>
    [HttpPut("{id:guid}/edit-content")]
    [Authorize]
    public async Task<IActionResult> UpdateWorkingCopy(
        Guid id, [FromBody] UpdateTutorialRequest request, CancellationToken ct)
    {
        var authorId = GetCurrentUserId()!.Value;
        var result = await _tutorialService.UpdateWorkingCopyAsync(id, request, authorId, ct);
        return Ok(result);
    }

    /// <summary>PUT /api/tutorials/{id}/submit-edit — Author submits working copy for contributor review.</summary>
    [HttpPut("{id:guid}/submit-edit")]
    [Authorize]
    public async Task<IActionResult> SubmitEdit(Guid id, CancellationToken ct)
    {
        var authorId = GetCurrentUserId()!.Value;
        var result = await _tutorialService.SubmitEditAsync(id, authorId, ct);
        return Ok(result);
    }

    /// <summary>PUT /api/tutorials/{id}/approve-edit — Manager approves edit: swaps content into original.</summary>
    [HttpPut("{id:guid}/approve-edit")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ManagerApproveEdit(Guid id, CancellationToken ct)
    {
        var managerId = GetCurrentUserId()!.Value;
        await _tutorialService.ManagerApproveEditAsync(id, managerId, ct);
        return Ok(new MessageResponse("Tutorial edit approved and published."));
    }

    /// <summary>PUT /api/tutorials/{id}/reject-edit — Manager rejects edit, returns working copy to author.</summary>
    [HttpPut("{id:guid}/reject-edit")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ManagerRejectEdit(
        Guid id, [FromBody] ManagerRejectRequest request, CancellationToken ct)
    {
        var managerId = GetCurrentUserId()!.Value;
        await _tutorialService.ManagerRejectEditAsync(id, managerId, request.Reason, ct);
        return Ok(new MessageResponse("Tutorial edit rejected. Author has been notified."));
    }

    // ── Manager final approval ───────────────────────────────────────────────

    /// <summary>PUT /api/tutorials/{id}/publish — Manager publishes a tutorial (BR-16).</summary>
    [HttpPut("{id:guid}/publish")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ManagerPublish(Guid id, CancellationToken ct)
    {
        var managerId = GetCurrentUserId()!.Value;
        await _tutorialService.ManagerPublishAsync(id, managerId, ct);
        return Ok(new MessageResponse("Tutorial has been published successfully."));
    }

    /// <summary>PUT /api/tutorials/{id}/reject — Manager rejects a tutorial (BR-16, BR-18).</summary>
    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ManagerReject(
        Guid id, [FromBody] ManagerRejectRequest request, CancellationToken ct)
    {
        var managerId = GetCurrentUserId()!.Value;
        await _tutorialService.ManagerRejectAsync(id, managerId, request.Reason, ct);
        return Ok(new MessageResponse("Tutorial has been rejected."));
    }

    /// <summary>DELETE /api/tutorials/{id} — Manager soft-removes a published tutorial (BR-16).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ManagerRemove(
        Guid id, [FromBody] ManagerRemoveRequest? request, CancellationToken ct)
    {
        var managerId = GetCurrentUserId()!.Value;
        await _tutorialService.ManagerRemoveAsync(id, managerId, request?.Reason, ct);
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
