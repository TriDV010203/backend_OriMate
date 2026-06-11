using MediatR;
using OrigamiPlatform.Application.Interfaces.Repositories;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Application.DTOs.Common;

namespace OrigamiPlatform.Application.Commands.Community;

public class ToggleLikeHandler : IRequestHandler<ToggleLikeCommand, ApiResponse<bool>>
{
    private readonly ICommunityRepository _repo;

    public ToggleLikeHandler(ICommunityRepository repo) => _repo = repo;

    public async Task<ApiResponse<bool>> Handle(ToggleLikeCommand command, CancellationToken ct)
    {
        var existingLike = await _repo.GetLikeAsync(command.UserId, command.TargetId, command.TargetType);

        if (existingLike != null)
        {
            // Unlike
            await _repo.RemoveLikeAsync(existingLike);
            return ApiResponse<bool>.Success(false, "Unliked successfully.");
        }
        else
        {
            // Like
            var newLike = new Like
            {
                UserId = command.UserId,
                TargetId = command.TargetId,
                TargetType = command.TargetType,
                CreatedAt = DateTime.UtcNow
            };
            await _repo.AddLikeAsync(newLike);
            return ApiResponse<bool>.Success(true, "Liked successfully.");
        }
    }
}