using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.CommunityPosts;

// DTO cho Media gửi từ Client
public class MediaItemDto
{
    public string MediaUrl { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
}

// Request từ frontend gửi lên (không chứa UserId vì UserId sẽ lấy từ Token)
public record CreateCommunityPostRequest(string Content, Guid? TutorialId, List<MediaItemDto>? MediaItems);