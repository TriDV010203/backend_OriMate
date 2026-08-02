
using OrigamiPlatform.Application.DTOs.CommunityPosts;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Queries.CommunityPosts;

public class GetCommunityFeedHandler
{
    private readonly ICommunityPostRepository _posts;
    private readonly ILikeRepository _likes;
    private readonly IFollowRepository _follows;
    private readonly ICommentRepository _comments;

    public GetCommunityFeedHandler(
        ICommunityPostRepository posts,
        ILikeRepository likes,
        IFollowRepository follows,
        ICommentRepository comments)
    {
        _posts = posts;
        _likes = likes;
        _follows = follows;
        _comments = comments;
    }

    public async Task<List<CommunityPostDto>> HandleAsync(GetCommunityFeedQuery query, CancellationToken ct = default)
    {
        var skip = (query.Page - 1) * query.PageSize;

        var followedUserIds = new List<Guid>();
        if (query.CurrentUserId.HasValue)
        {
            followedUserIds = await _follows.GetFollowingIdsAsync(query.CurrentUserId.Value, ct);
        }

        var posts = await _posts.GetCommunityFeedAsync(followedUserIds, skip, query.PageSize);

        var result = new List<CommunityPostDto>();

        foreach (var post in posts)
        {
            var likeCount = await _likes.GetLikeCountAsync(post.Id, TargetType.CommunityPost);

            var commentCount = await _comments.GetCommentCountAsync(post.Id, TargetType.CommunityPost, ct);

            bool isLiked = false;
            if (query.CurrentUserId.HasValue)
            {
                var likeRecord = await _likes.GetLikeAsync(query.CurrentUserId.Value, post.Id, TargetType.CommunityPost);
                isLiked = likeRecord != null;
            }

            bool isFromFollowed = followedUserIds.Contains(post.AuthorId);

            var dto = new CommunityPostDto(
                Id: post.Id,
                AuthorId: post.AuthorId,
                Content: post.Content,
                CreatedAt: post.CreatedAt,
                CommentCount: commentCount,
                LikeCount: likeCount,
                IsLikedByCurrentUser: isLiked,
                IsFromFollowedCreator: isFromFollowed, 
                Media: post.Media.OrderBy(m => m.DisplayOrder).Select(m => new MediaItemDto
                {
                    MediaUrl = m.Url,
                    MediaType = m.MediaType
                }).ToList()
            );

            result.Add(dto);
        }

        return result;
    }
}