using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.LearningPaths;
using OrigamiPlatform.Application.DTOs;
using OrigamiPlatform.Application.DTOs.LearningPaths;
using OrigamiPlatform.Application.Queries.LearningPaths;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/learning-paths")]
public class LearningPathsController : ControllerBase
{
    private readonly GetLearningPathsHandler _getLearningPaths;
    private readonly GetLearningPathByIdHandler _getLearningPathById;
    private readonly GetLearningPathForTutorialHandler _getLearningPathForTutorial;
    private readonly GetAdminLearningPathsHandler _getAdminLearningPaths;
    private readonly GetLearningPathForAdminHandler _getLearningPathForAdmin;
    private readonly CreateLearningPathHandler _createLearningPath;
    private readonly UpdateLearningPathHandler _updateLearningPath;
    private readonly PublishLearningPathHandler _publishLearningPath;
    private readonly ArchiveLearningPathHandler _archiveLearningPath;

    public LearningPathsController(
        GetLearningPathsHandler getLearningPaths,
        GetLearningPathByIdHandler getLearningPathById,
        GetLearningPathForTutorialHandler getLearningPathForTutorial,
        GetAdminLearningPathsHandler getAdminLearningPaths,
        GetLearningPathForAdminHandler getLearningPathForAdmin,
        CreateLearningPathHandler createLearningPath,
        UpdateLearningPathHandler updateLearningPath,
        PublishLearningPathHandler publishLearningPath,
        ArchiveLearningPathHandler archiveLearningPath)
    {
        _getLearningPaths = getLearningPaths;
        _getLearningPathById = getLearningPathById;
        _getLearningPathForTutorial = getLearningPathForTutorial;
        _getAdminLearningPaths = getAdminLearningPaths;
        _getLearningPathForAdmin = getLearningPathForAdmin;
        _createLearningPath = createLearningPath;
        _updateLearningPath = updateLearningPath;
        _publishLearningPath = publishLearningPath;
        _archiveLearningPath = archiveLearningPath;
    }

    // ── Public ───────────────────────────────────────────────────────────────

    /// <summary>GET /api/learning-paths — Published learning paths only.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetLearningPaths(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _getLearningPaths.HandleAsync(new GetLearningPathsQuery(search, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>GET /api/learning-paths/{id} — Published learning path detail with ordered tutorials.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLearningPathById(Guid id, CancellationToken ct)
    {
        var result = await _getLearningPathById.HandleAsync(new GetLearningPathByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>GET /api/learning-paths/for-tutorial/{tutorialId} — Which published path (if any) this tutorial belongs to, and its position in it. 200 with a null body when it isn't part of any path (not an error).</summary>
    [HttpGet("for-tutorial/{tutorialId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLearningPathForTutorial(Guid tutorialId, CancellationToken ct)
    {
        var result = await _getLearningPathForTutorial.HandleAsync(new GetLearningPathForTutorialQuery(tutorialId), ct);
        return Ok(result);
    }

    // ── Admin management ─────────────────────────────────────────────────────

    /// <summary>GET /api/learning-paths/admin/all — Every learning path (any status), for the admin management page.</summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetAdminLearningPaths(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _getAdminLearningPaths.HandleAsync(
            new GetAdminLearningPathsQuery(search, status, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>GET /api/learning-paths/{id}/admin — Any learning path's detail (any status), to pre-fill the admin edit form.</summary>
    [HttpGet("{id:guid}/admin")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetLearningPathForAdmin(Guid id, CancellationToken ct)
    {
        var result = await _getLearningPathForAdmin.HandleAsync(new GetLearningPathForAdminQuery(id), ct);
        return Ok(result);
    }

    /// <summary>POST /api/learning-paths — Admin/Manager creates a new learning path as Draft.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateLearningPath(
        [FromBody] CreateLearningPathRequest request, CancellationToken ct)
    {
        var actorId = GetCurrentUserId();
        var result = await _createLearningPath.HandleAsync(new CreateLearningPathCommand(actorId, request), ct);
        return CreatedAtAction(nameof(GetLearningPathForAdmin), new { id = result.Id }, result);
    }

    /// <summary>PUT /api/learning-paths/{id} — Admin/Manager edits a learning path's content and item list.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateLearningPath(
        Guid id, [FromBody] UpdateLearningPathRequest request, CancellationToken ct)
    {
        var actorId = GetCurrentUserId();
        var result = await _updateLearningPath.HandleAsync(new UpdateLearningPathCommand(id, actorId, request), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/learning-paths/{id}/publish — Admin/Manager publishes a learning path (needs ≥1 tutorial).</summary>
    [HttpPut("{id:guid}/publish")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> PublishLearningPath(Guid id, CancellationToken ct)
    {
        var actorId = GetCurrentUserId();
        await _publishLearningPath.HandleAsync(new PublishLearningPathCommand(id, actorId), ct);
        return Ok(new MessageResponse("Learning path has been published."));
    }

    /// <summary>PUT /api/learning-paths/{id}/archive — Admin/Manager archives a learning path (soft-delete equivalent).</summary>
    [HttpPut("{id:guid}/archive")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ArchiveLearningPath(Guid id, CancellationToken ct)
    {
        var actorId = GetCurrentUserId();
        await _archiveLearningPath.HandleAsync(new ArchiveLearningPathCommand(id, actorId), ct);
        return Ok(new MessageResponse("Learning path has been archived."));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.Parse(value!);
    }
}
