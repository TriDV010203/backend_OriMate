using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Journals;
using OrigamiPlatform.Application.DTOs.Journals;
using OrigamiPlatform.Application.Queries.Journals;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/journals")]
public class JournalsController : ControllerBase
{
    private readonly CreateJournalHandler _createJournal;
    private readonly UpdateJournalHandler _updateJournal;
    private readonly DeleteJournalHandler _deleteJournal;
    private readonly GetUserJournalsHandler _getUserJournals;

    public JournalsController(
        CreateJournalHandler createJournal,
        UpdateJournalHandler updateJournal,
        DeleteJournalHandler deleteJournal,
        GetUserJournalsHandler getUserJournals)
        => (_createJournal, _updateJournal, _deleteJournal, _getUserJournals)
            = (createJournal, updateJournal, deleteJournal, getUserJournals);

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        CreateJournalRequest request,
        CancellationToken ct)
    {
        var result = await _createJournal.HandleAsync(
            new CreateJournalCommand(GetCurrentUserId(), request),
            ct);

        return CreatedAtAction(
            nameof(GetUserJournals),
            new { userId = result.UserId },
            result);
    }

    [HttpPut("{journalId:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(
        Guid journalId,
        UpdateJournalRequest request,
        CancellationToken ct)
    {
        var result = await _updateJournal.HandleAsync(
            new UpdateJournalCommand(GetCurrentUserId(), journalId, request),
            ct);

        return Ok(result);
    }

    [HttpDelete("{journalId:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid journalId, CancellationToken ct)
    {
        await _deleteJournal.HandleAsync(
            new DeleteJournalCommand(GetCurrentUserId(), journalId),
            ct);

        return NoContent();
    }

    [HttpGet("/api/users/{userId:guid}/journals")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserJournals(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var result = await _getUserJournals.HandleAsync(
            new GetUserJournalsQuery(userId, GetOptionalCurrentUserId(), page, pageSize),
            ct);

        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyJournals(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _getUserJournals.HandleAsync(
            new GetUserJournalsQuery(userId, userId, page, pageSize),
            ct);

        return Ok(result);
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
