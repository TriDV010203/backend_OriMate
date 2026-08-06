using OrigamiPlatform.Application.DTOs.Users;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.Users;

public class GetFeaturedCreatorsHandler
{
    private readonly IFollowRepository _follows;
    private readonly ITutorialRepository _tutorials;

    public GetFeaturedCreatorsHandler(IFollowRepository follows, ITutorialRepository tutorials)
    {
        _follows = follows;
        _tutorials = tutorials;
    }

    public async Task<List<FollowerUserDto>> HandleAsync(GetFeaturedCreatorsQuery query, CancellationToken ct = default)
    {
        var users = await _follows.GetTopFollowedUsersAsync(query.Count, ct);

        var items = new List<FollowerUserDto>();
        foreach (var user in users)
        {
            var followerCount = await _follows.GetFollowersCountAsync(user.Id, ct);
            var tutorials = await _tutorials.GetByAuthorAsync(user.Id, 1, 1, ct);

            bool isFollowing = false;
            if (query.CurrentUserId.HasValue && query.CurrentUserId.Value != Guid.Empty)
            {
                var followRecord = await _follows.GetFollowAsync(query.CurrentUserId.Value, user.Id, ct);
                isFollowing = followRecord != null;
            }

            items.Add(new FollowerUserDto(
                UserId: user.Id,
                DisplayName: user.Profile?.DisplayName ?? "Unknown Creator",
                AvatarUrl: user.Profile?.AvatarUrl,
                Bio: user.Profile?.Bio,
                FollowerCount: followerCount,
                TutorialCount: tutorials.TotalCount,
                IsFollowing: isFollowing,
                Roles: user.Roles.Select(r => r.Role.ToString()).ToList()
            ));
        }

        return items;
    }
}
