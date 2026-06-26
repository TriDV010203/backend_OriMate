using OrigamiPlatform.Application.DTOs.Users;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.Users;

public class GetCreatorProfileHandler
{
    private readonly IUserRepository _users;
    private readonly IFollowRepository _follows;
    private readonly ICommunityPostRepository _posts;
    private readonly IAchievementRepository _achievements;

    public GetCreatorProfileHandler(
        IUserRepository users,
        IFollowRepository follows,
        ICommunityPostRepository posts,
        IAchievementRepository achievements)
    {
        _users = users;
        _follows = follows;
        _posts = posts;
        _achievements = achievements;
    }

    public async Task<CreatorProfileDto> HandleAsync(GetCreatorProfileQuery query, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(query.TargetUserId, ct);
        if (user == null)
        {
            throw new NotFoundException("Creator profile not found.");
        }

        var followerCount = await _follows.GetFollowersCountAsync(query.TargetUserId, ct);
        var followingCount = await _follows.GetFollowingCountAsync(query.TargetUserId, ct);

        var postCount = await _posts.GetPostCountByAuthorAsync(query.TargetUserId, ct);

        var achievementsData = await _achievements.GetByUserAsync(
            userId: query.TargetUserId,
            includePrivate: false,
            page: 1,
            pageSize: 1,
            ct: ct);
        var achievementCount = achievementsData.TotalCount;

        bool isFollowing = false;
        if (query.CurrentUserId.HasValue && query.CurrentUserId.Value != Guid.Empty)
        {
            var followRecord = await _follows.GetFollowAsync(query.CurrentUserId.Value, query.TargetUserId, ct);
            isFollowing = followRecord != null;
        }

        return new CreatorProfileDto(
            UserId: user.Id,
            DisplayName: user.Profile?.DisplayName ?? "Unknown Creator",
            AvatarUrl: user.Profile?.AvatarUrl,
            Bio: user.Profile?.Bio,
            FollowerCount: followerCount,
            FollowingCount: followingCount,
            PostCount: postCount,
            AchievementCount: achievementCount,
            IsFollowing: isFollowing,
            IsSuspended: user.Status == AccountStatus.Suspended,
            Roles: user.Roles.Select(r => r.Role.ToString()).ToList()
        );
    }
}