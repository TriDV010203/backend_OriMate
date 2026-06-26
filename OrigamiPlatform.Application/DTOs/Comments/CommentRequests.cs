using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.Comments;

public record AddCommentRequest(
    Guid TargetId,
    TargetType TargetType, // 0: CommunityPost, 1: Tutorial
    string Content
);