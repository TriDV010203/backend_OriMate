using MediatR;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Commands.Community;

public record ToggleLikeCommand(Guid TargetId, TargetType TargetType, Guid UserId) : IRequest<ApiResponse<bool>>;