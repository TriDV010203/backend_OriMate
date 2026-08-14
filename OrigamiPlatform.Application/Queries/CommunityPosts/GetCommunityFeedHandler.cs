using OrigamiPlatform.Application.DTOs.CommunityPosts;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Application.Queries.CommunityPosts;

public class GetCommunityFeedHandler
{
    private readonly ICommunityPostRepository _posts;
    private readonly IFollowRepository _follows;

    public GetCommunityFeedHandler(
        ICommunityPostRepository posts,
        IFollowRepository follows)
    {
        _posts = posts;
        _follows = follows;
    }

    public async Task<List<CommunityPostDto>> HandleAsync(GetCommunityFeedQuery query, CancellationToken ct = default)
    {
        var skip = (query.Page - 1) * query.PageSize;

        var followedUserIds = new List<Guid>();
        if (query.CurrentUserId.HasValue)
        {
            followedUserIds = await _follows.GetFollowingIdsAsync(query.CurrentUserId.Value, ct);
        }

        return await _posts.GetCommunityFeedListAsync(followedUserIds, query.CurrentUserId, skip, query.PageSize, ct);
    }
}
