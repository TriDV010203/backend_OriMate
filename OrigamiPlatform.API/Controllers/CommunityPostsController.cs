using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.CommunityPosts;
using OrigamiPlatform.Application.DTOs.CommunityPosts;
using OrigamiPlatform.Application.Queries.CommunityPosts;
using System.Security.Claims;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/community-posts")]
[Authorize]
public class CommunityPostsController : ControllerBase
{
    private readonly CreateCommunityPostHandler _createPost;

    public CommunityPostsController(CreateCommunityPostHandler createPost)
        => _createPost = createPost;

    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreateCommunityPostRequest request, CancellationToken ct)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out Guid userId))
            return Unauthorized();

        var command = new CreateCommunityPostCommand(userId, request.Content, request.TutorialId, request.MediaItems);
        var postId = await _createPost.HandleAsync(command, ct);

        return Ok(new { PostId = postId });
    }


    [HttpGet("feed")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeed(
        [FromServices] GetCommunityFeedHandler getFeedHandler,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        Guid? currentUserId = null;
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdString, out Guid parsedId))
        {
            currentUserId = parsedId;
        }

        var query = new GetCommunityFeedQuery(currentUserId, page, pageSize);
        var result = await getFeedHandler.HandleAsync(query, ct);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] GetCommunityPostByIdHandler getByIdHandler,
        CancellationToken ct)
    {
        Guid? currentUserId = null;
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdString, out Guid parsedId))
        {
            currentUserId = parsedId;
        }

        var query = new GetCommunityPostByIdQuery(id, currentUserId);
        var result = await getByIdHandler.HandleAsync(query, ct);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}