using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.DTOs.Likes;
public record ToggleLikeRequest(Guid TargetId, TargetType TargetType);