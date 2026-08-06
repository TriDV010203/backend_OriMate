using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.LearningPathModes;
using OrigamiPlatform.Application.DTOs;
using OrigamiPlatform.Application.DTOs.LearningPathModes;
using OrigamiPlatform.Application.Queries.LearningPathModes;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/learning-path-modes")]
public class LearningPathModesController : ControllerBase
{
    private readonly GetLearningPathModesHandler _getModes;
    private readonly GetAdminLearningPathModesHandler _getAdminModes;
    private readonly GetModeUnlockSubmissionsHandler _getSubmissions;
    private readonly CreateLearningPathModeHandler _createMode;
    private readonly UpdateLearningPathModeHandler _updateMode;
    private readonly UpsertModeUnlockTestHandler _upsertUnlockTest;
    private readonly SubmitModeUnlockTestHandler _submitUnlockTest;
    private readonly ApproveModeUnlockSubmissionHandler _approveSubmission;
    private readonly RejectModeUnlockSubmissionHandler _rejectSubmission;

    public LearningPathModesController(
        GetLearningPathModesHandler getModes,
        GetAdminLearningPathModesHandler getAdminModes,
        GetModeUnlockSubmissionsHandler getSubmissions,
        CreateLearningPathModeHandler createMode,
        UpdateLearningPathModeHandler updateMode,
        UpsertModeUnlockTestHandler upsertUnlockTest,
        SubmitModeUnlockTestHandler submitUnlockTest,
        ApproveModeUnlockSubmissionHandler approveSubmission,
        RejectModeUnlockSubmissionHandler rejectSubmission)
    {
        _getModes = getModes;
        _getAdminModes = getAdminModes;
        _getSubmissions = getSubmissions;
        _createMode = createMode;
        _updateMode = updateMode;
        _upsertUnlockTest = upsertUnlockTest;
        _submitUnlockTest = submitUnlockTest;
        _approveSubmission = approveSubmission;
        _rejectSubmission = rejectSubmission;
    }

    // ── Public ───────────────────────────────────────────────────────────────

    /// <summary>GET /api/learning-path-modes — Roadmap tabs. Includes the caller's unlock/submission
    /// status when authenticated; anonymous callers only see which mode is the (always-open) entry mode.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetLearningPathModes(CancellationToken ct)
    {
        var result = await _getModes.HandleAsync(new GetLearningPathModesQuery(GetCurrentUserIdOrNull()), ct);
        return Ok(result);
    }

    /// <summary>POST /api/learning-path-modes/{modeId}/unlock-test/submissions — Submit the unlock
    /// test photo for modeId. Independent of Achievement.</summary>
    [HttpPost("{modeId:guid}/unlock-test/submissions")]
    [Authorize]
    public async Task<IActionResult> SubmitUnlockTest(
        Guid modeId, [FromBody] SubmitModeUnlockTestRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _submitUnlockTest.HandleAsync(new SubmitModeUnlockTestCommand(userId, modeId, request), ct);
        return Ok(result);
    }

    // ── Admin management ─────────────────────────────────────────────────────

    /// <summary>GET /api/learning-path-modes/admin/all — Every mode, including inactive ones.</summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetAdminLearningPathModes(CancellationToken ct)
    {
        var result = await _getAdminModes.HandleAsync(new GetAdminLearningPathModesQuery(), ct);
        return Ok(result);
    }

    /// <summary>POST /api/learning-path-modes — Create a new mode.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateLearningPathMode(
        [FromBody] CreateLearningPathModeRequest request, CancellationToken ct)
    {
        var result = await _createMode.HandleAsync(new CreateLearningPathModeCommand(request), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/learning-path-modes/{id} — Update a mode's name/description/order/active state.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateLearningPathMode(
        Guid id, [FromBody] UpdateLearningPathModeRequest request, CancellationToken ct)
    {
        var result = await _updateMode.HandleAsync(new UpdateLearningPathModeCommand(id, request), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/learning-path-modes/{modeId}/unlock-test — Assign/replace the tutorial used as modeId's unlock test.</summary>
    [HttpPut("{modeId:guid}/unlock-test")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpsertUnlockTest(
        Guid modeId, [FromBody] UpsertModeUnlockTestRequest request, CancellationToken ct)
    {
        await _upsertUnlockTest.HandleAsync(new UpsertModeUnlockTestCommand(modeId, request), ct);
        return Ok(new MessageResponse("Unlock test saved."));
    }

    /// <summary>GET /api/learning-path-modes/admin/unlock-test-submissions — Review queue.</summary>
    [HttpGet("admin/unlock-test-submissions")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetUnlockTestSubmissions(
        [FromQuery] Guid? modeId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _getSubmissions.HandleAsync(
            new GetModeUnlockSubmissionsQuery(modeId, status, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/learning-path-modes/unlock-test-submissions/{id}/approve — Approve a pending submission, unlocking the mode for that user.</summary>
    [HttpPut("unlock-test-submissions/{id:guid}/approve")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> ApproveSubmission(Guid id, CancellationToken ct)
    {
        var actorId = GetCurrentUserId();
        await _approveSubmission.HandleAsync(new ApproveModeUnlockSubmissionCommand(id, actorId), ct);
        return Ok(new MessageResponse("Submission approved."));
    }

    /// <summary>PUT /api/learning-path-modes/unlock-test-submissions/{id}/reject — Reject a pending submission with a reason.</summary>
    [HttpPut("unlock-test-submissions/{id:guid}/reject")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RejectSubmission(
        Guid id, [FromBody] RejectModeUnlockSubmissionRequest request, CancellationToken ct)
    {
        var actorId = GetCurrentUserId();
        await _rejectSubmission.HandleAsync(new RejectModeUnlockSubmissionCommand(id, actorId, request.Reason), ct);
        return Ok(new MessageResponse("Submission rejected."));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.Parse(value!);
    }

    private Guid? GetCurrentUserIdOrNull()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return value is null ? null : Guid.Parse(value);
    }
}
