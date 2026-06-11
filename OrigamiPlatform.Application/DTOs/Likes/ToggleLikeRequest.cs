using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.Likes;

// Request Frontend gửi lên chỉ cần TargetId và TargetType. UserId sẽ lấy từ Token.
public record ToggleLikeRequest(Guid TargetId, TargetType TargetType);