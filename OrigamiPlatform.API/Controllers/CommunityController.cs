using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OrigamiPlatform.Application.Commands.Community;
using OrigamiPlatform.Application.DTOs.Community;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize] // Yêu cầu xác thực cho tất cả các endpoint trong nhóm Community
public class CommunityController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommunityController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Đăng bài viết mới vào Community Feed
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
    {
        var userId = GetCurrentUserId();
        var command = new CreatePostCommand(request, userId);

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(result)
            : BadRequest(result);
    }

    /// <summary>
    /// Thích hoặc Bỏ thích bài viết / Tutorial
    /// </summary>
    [HttpPost("{targetId}/like")]
    public async Task<IActionResult> ToggleLike(Guid targetId, [FromQuery] TargetType targetType)
    {
        var command = new ToggleLikeCommand(targetId, targetType, GetCurrentUserId());
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}