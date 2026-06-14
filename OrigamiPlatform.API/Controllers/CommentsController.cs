using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Comments;
using OrigamiPlatform.Application.DTOs.Comments;
using OrigamiPlatform.Application.Queries.Comments;
using OrigamiPlatform.Domain.Enums;
using System.Security.Claims;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetComments(
        [FromServices] GetCommentsHandler getComments,
        [FromQuery] Guid targetId,
        [FromQuery] TargetType targetType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new GetCommentsQuery(targetId, targetType, page, pageSize);
        var result = await getComments.HandleAsync(query, ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddComment(
        [FromServices] AddCommentHandler addComment,
        [FromBody] AddCommentRequest request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var command = new AddCommentCommand(userId, request.TargetId, request.TargetType, request.Content);

        var commentId = await addComment.HandleAsync(command, ct);
        return Ok(new { CommentId = commentId });
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(
        [FromServices] DeleteCommentHandler deleteComment,
        Guid id,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var command = new DeleteCommentCommand(userId, id);

        await deleteComment.HandleAsync(command, ct);
        return Ok(new { Message = "Comment deleted successfully." });
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(value!);
    }
}