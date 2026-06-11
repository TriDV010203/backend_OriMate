using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Commands.Likes;

// Command nhận vào UserId (từ token), Id của mục được like, và loại mục đó (Tutorial hay CommunityPost)
public record ToggleLikeCommand(Guid UserId, Guid TargetId, TargetType TargetType);