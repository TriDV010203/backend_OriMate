using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.CommunityPosts;
using OrigamiPlatform.Application.DTOs.CommunityPosts;
using OrigamiPlatform.Application.Queries.CommunityPosts;
using System.Security.Claims;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/community-posts")]
[Authorize] // Bắt buộc đăng nhập (Ngăn Guest - NAC-03)
public class CommunityPostsController : ControllerBase
{
    private readonly CreateCommunityPostHandler _createPost;

    public CommunityPostsController(CreateCommunityPostHandler createPost)
        => _createPost = createPost;

    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreateCommunityPostRequest request, CancellationToken ct)
    {
        // Trích xuất UserId từ JWT Token
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out Guid userId))
            return Unauthorized();

        // Tạo Command và gọi Handler
        var command = new CreateCommunityPostCommand(userId, request.Content, request.TutorialId, request.MediaItems);
        var postId = await _createPost.HandleAsync(command, ct);

        // Trả về HTTP 200 OK kèm Id của bài viết mới
        return Ok(new { PostId = postId });
    }

    // Đầu file nhớ thêm: using OrigamiPlatform.Application.Queries.CommunityPosts;

    [HttpGet("feed")]
    [AllowAnonymous] // Bảng tin thì khách chưa đăng nhập cũng xem được, chỉ không like/comment được
    public async Task<IActionResult> GetFeed(
        [FromServices] GetCommunityFeedHandler getFeedHandler,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        // Thử lấy UserId nếu họ ĐÃ đăng nhập (để tính IsLikedByCurrentUser)
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
}