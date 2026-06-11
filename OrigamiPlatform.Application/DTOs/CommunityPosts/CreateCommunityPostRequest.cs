using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.CommunityPosts;

public class MediaItemDto
{
    public string MediaUrl { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
}

public class CreateCommunityPostRequest
{
    public string Content { get; set; } = string.Empty;
    public Guid? TutorialId { get; set; }
    public List<MediaItemDto>? MediaItems { get; set; }
}