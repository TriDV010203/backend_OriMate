using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.WeeklyChallenge;
using OrigamiPlatform.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/weekly-challenges")]
public class WeeklyChallengeController : ControllerBase
{
    private readonly IWeeklyChallengeService _service;

    public WeeklyChallengeController(IWeeklyChallengeService service)
    {
        _service = service;
    }

    // --- Admin Endpoints ---

    [Authorize(Roles = "Admin,Manager")]
    [HttpGet("admin")]
    public async Task<ActionResult<PagedResult<WeeklyChallengeDto>>> GetAdminChallenges([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(await _service.GetAdminChallengesAsync(page, pageSize));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPost("admin")]
    public async Task<ActionResult<WeeklyChallengeDto>> CreateChallenge([FromBody] CreateWeeklyChallengeDto dto)
    {
        var adminId = GetCurrentUserId();
        var result = await _service.CreateChallengeAsync(dto, adminId);
        return Created($"/api/weekly-challenges/{result.Id}", result);
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpPut("admin/{id}")]
    public async Task<ActionResult<WeeklyChallengeDto>> UpdateChallenge(Guid id, [FromBody] UpdateWeeklyChallengeDto dto)
    {
        return Ok(await _service.UpdateChallengeAsync(id, dto));
    }

    [Authorize(Roles = "Admin,Manager")]
    [HttpDelete("admin/{id}")]
    public async Task<ActionResult> DeleteChallenge(Guid id)
    {
        await _service.DeleteChallengeAsync(id);
        return NoContent();
    }

    // --- User Endpoints ---

    [HttpGet("current")]
    public async Task<ActionResult<WeeklyChallengeDto>> GetCurrentChallenge()
    {
        var userId = GetOptionalCurrentUserId();
        var challenge = await _service.GetCurrentChallengeAsync(userId);
        if (challenge == null) return NotFound("Hiện tại chưa có thử thách tuần nào.");
        return Ok(challenge);
    }

    [HttpGet("{id}/submissions")]
    public async Task<ActionResult<PagedResult<WeeklyChallengeSubmissionDto>>> GetSubmissions(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetOptionalCurrentUserId();
        return Ok(await _service.GetSubmissionsAsync(id, page, pageSize, userId));
    }

    [Authorize]
    [HttpPost("{id}/submit")]
    public async Task<ActionResult<WeeklyChallengeSubmissionDto>> Submit(Guid id, [FromBody] SubmitWeeklyChallengeDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _service.SubmitAsync(id, dto, userId);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("submissions/{submissionId}/toggle-like")]
    public async Task<ActionResult> ToggleLike(Guid submissionId)
    {
        var userId = GetCurrentUserId();
        await _service.ToggleSubmissionLikeAsync(submissionId, userId);
        return Ok();
    }


    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var userId))
            throw new ForbiddenException("Invalid user token.");

        return userId;
    }

    private Guid? GetOptionalCurrentUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
