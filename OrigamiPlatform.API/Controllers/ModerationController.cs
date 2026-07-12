using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Moderation;
using OrigamiPlatform.Application.DTOs;
using OrigamiPlatform.Application.DTOs.Moderation;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/moderation")]
public class ModerationController : ControllerBase
{
    private readonly DeleteViolatingCommentHandler _deleteViolatingComment;

    public ModerationController(DeleteViolatingCommentHandler deleteViolatingComment)
        => _deleteViolatingComment = deleteViolatingComment;

    /// <summary>DELETE /api/moderation/comments/{commentId} — Contributor Reviewer/Manager/Admin removes a clearly violating comment directly (BR-COMM-02, no Report queue).</summary>
    [HttpDelete("comments/{commentId:guid}")]
    [Authorize(Roles = "ContributorReviewer,Manager,Admin")]
    public async Task<IActionResult> DeleteViolatingComment(
        Guid commentId, [FromBody] DeleteViolatingCommentRequest request, CancellationToken ct)
    {
        var moderatorId = GetCurrentUserId();
        await _deleteViolatingComment.HandleAsync(
            new DeleteViolatingCommentCommand(moderatorId, commentId, request.Reason), ct);
        return Ok(new MessageResponse("Comment has been removed."));
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                 ?? throw new ForbiddenException("User identity not found in token.");
        return Guid.Parse(value);
    }
}
