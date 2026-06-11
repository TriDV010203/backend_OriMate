using OrigamiPlatform.Application.DTOs.CommunityPosts;

namespace OrigamiPlatform.Application.Commands.CommunityPosts;

public record CreateCommunityPostCommand(
    Guid UserId,
    string Content,
    Guid? TutorialId,
    List<MediaItemDto>? MediaItems);