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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var result = await _getTutorials.HandleAsync(
            new GetTutorialsQuery(search, categoryId, difficulty, type, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await _getTutorialBySlug.HandleAsync(
            new GetTutorialBySlugQuery(slug), ct);
        return Ok(result);
    }

    // ── Authoring ────────────────────────────────────────────────────────────

    /// <summary>POST /api/tutorials — Create a draft tutorial (any authenticated user).</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateTutorial(
        [FromBody] CreateTutorialRequest request, CancellationToken ct)
    {
        var authorId = GetCurrentUserId();
        var result = await _tutorialService.CreateDraftAsync(request, authorId, ct);
        return CreatedAtAction(nameof(GetBySlug), new { slug = result.Slug }, result);
    }

    /// <summary>PUT /api/tutorials/{id}/submit — Submit draft for contributor review (author only).</summary>
    [HttpPut("{id:guid}/submit")]
    [Authorize]
    public async Task<IActionResult> SubmitForReview(Guid id, CancellationToken ct)
    {
        var authorId = GetCurrentUserId();
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
        var authorId = GetCurrentUserId();
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
        var reviewerId = GetCurrentUserId();
        await _tutorialService.ContributorApproveAsync(id, reviewerId, ct);
        return Ok(new MessageResponse("Tutorial approved and sent to manager review."));
    }

    /// <summary>PUT /api/tutorials/{id}/contributor-request-revision — Contributor requests revision from author.</summary>
    [HttpPut("{id:guid}/contributor-request-revision")]
    [Authorize(Roles = "ContributorReviewer")]
    public async Task<IActionResult> ContributorRequestRevision(
        Guid id, [FromBody] ReviewActionRequest request, CancellationToken ct)
    {
        var reviewerId = GetCurrentUserId();
        await _tutorialService.ContributorRequestRevisionAsync(id, reviewerId, request.Reason, ct);
        return Ok(new MessageResponse("Revision request sent to the tutorial author."));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.Parse(value!);
    }
}
