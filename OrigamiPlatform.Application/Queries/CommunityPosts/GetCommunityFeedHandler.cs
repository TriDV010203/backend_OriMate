//using OrigamiPlatform.Application.DTOs.CommunityPosts;
//using OrigamiPlatform.Application.Interfaces;
//using OrigamiPlatform.Domain.Enums;

//namespace OrigamiPlatform.Application.Queries.CommunityPosts;

//public class GetCommunityFeedHandler
//{
//    private readonly ICommunityPostRepository _posts;
//    private readonly ILikeRepository _likes;

//    public GetCommunityFeedHandler(ICommunityPostRepository posts, ILikeRepository likes)
//    {
//        _posts = posts;
//        _likes = likes;
//    }

//    public async Task<List<CommunityPostDto>> HandleAsync(GetCommunityFeedQuery query, CancellationToken ct = default)
//    {
//        var skip = (query.Page - 1) * query.PageSize;
//        var posts = await _posts.GetApprovedPostsAsync(skip, query.PageSize);

//        var result = new List<CommunityPostDto>();

//        foreach (var post in posts)
//        {
//            var likeCount = await _likes.GetLikeCountAsync(post.Id, TargetType.CommunityPost);

//            bool isLiked = false;
//            if (query.CurrentUserId.HasValue)
//            {
//                var likeRecord = await _likes.GetLikeAsync(query.CurrentUserId.Value, post.Id, TargetType.CommunityPost);
//                isLiked = likeRecord != null;
//            }

//            var dto = new CommunityPostDto(
//                Id: post.Id,
//                AuthorId: post.AuthorId,
//                Content: post.Content,
//                CreatedAt: post.CreatedAt,
//                CommentCount: post.Comments?.Count ?? 0,
//                LikeCount: likeCount,
//                IsLikedByCurrentUser: isLiked,
//                Media: post.Media.OrderBy(m => m.DisplayOrder).Select(m => new MediaItemDto
//                {
//                    MediaUrl = m.Url,
//                    MediaType = m.MediaType
//                }).ToList()
//            );

//            result.Add(dto);
//        }

//        return result;
//    }
//}
using OrigamiPlatform.Application.DTOs.CommunityPosts;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Queries.CommunityPosts;

public class GetCommunityFeedHandler
{
    private readonly ICommunityPostRepository _posts;
    private readonly ILikeRepository _likes;
    private readonly IFollowRepository _follows; // Inject thêm FollowRepo

    public GetCommunityFeedHandler(
        ICommunityPostRepository posts,
        ILikeRepository likes,
        IFollowRepository follows)
    {
        _posts = posts;
        _likes = likes;
        _follows = follows;
    }

    public async Task<List<CommunityPostDto>> HandleAsync(GetCommunityFeedQuery query, CancellationToken ct = default)
    {
        var skip = (query.Page - 1) * query.PageSize;

        // 1. Lấy danh sách ID những người user đang follow
        var followedUserIds = new List<Guid>();
        if (query.CurrentUserId.HasValue)
        {
            followedUserIds = await _follows.GetFollowingIdsAsync(query.CurrentUserId.Value, ct);
        }

        // 2. Kéo danh sách bài viết từ DB với thuật toán sắp xếp vừa viết
        var posts = await _posts.GetCommunityFeedAsync(followedUserIds, skip, query.PageSize);

        var result = new List<CommunityPostDto>();

        // 3. Map dữ liệu
        foreach (var post in posts)
        {
            var likeCount = await _likes.GetLikeCountAsync(post.Id, TargetType.CommunityPost);

            bool isLiked = false;
            if (query.CurrentUserId.HasValue)
            {
                var likeRecord = await _likes.GetLikeAsync(query.CurrentUserId.Value, post.Id, TargetType.CommunityPost);
                isLiked = likeRecord != null;
            }

            // Kiểm tra xem bài này có phải của người mình follow không
            bool isFromFollowed = followedUserIds.Contains(post.AuthorId);

            var dto = new CommunityPostDto(
                Id: post.Id,
                AuthorId: post.AuthorId,
                Content: post.Content,
                CreatedAt: post.CreatedAt,
                CommentCount: post.Comments?.Count ?? 0,
                LikeCount: likeCount,
                IsLikedByCurrentUser: isLiked,
                IsFromFollowedCreator: isFromFollowed, // Để Frontend dán nhãn theo AC-01
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