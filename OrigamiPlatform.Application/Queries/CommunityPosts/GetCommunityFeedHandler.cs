using OrigamiPlatform.Application.DTOs.CommunityPosts;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Queries.CommunityPosts;

public class GetCommunityFeedHandler
{
    private readonly ICommunityPostRepository _posts;
    private readonly ILikeRepository _likes;

    public GetCommunityFeedHandler(ICommunityPostRepository posts, ILikeRepository likes)
    {
        _posts = posts;
        _likes = likes;
    }

    public async Task<List<CommunityPostDto>> HandleAsync(GetCommunityFeedQuery query, CancellationToken ct = default)
    {
        var skip = (query.Page - 1) * query.PageSize;
        var posts = await _posts.GetApprovedPostsAsync(skip, query.PageSize);

        var result = new List<CommunityPostDto>();

        foreach (var post in posts)
        {
            // Đếm Like
            var likeCount = await _likes.GetLikeCountAsync(post.Id, TargetType.CommunityPost);

            // Kiểm tra User hiện tại có like bài này không
            bool isLiked = false;
            if (query.CurrentUserId.HasValue)
            {
                var likeRecord = await _likes.GetLikeAsync(query.CurrentUserId.Value, post.Id, TargetType.CommunityPost);
                isLiked = likeRecord != null;
            }

            // Map Entity sang DTO
            var dto = new CommunityPostDto(
                Id: post.Id,
                AuthorId: post.AuthorId,
                Content: post.Content,
                CreatedAt: post.CreatedAt,
                CommentCount: post.Comments?.Count ?? 0,
                LikeCount: likeCount,
                IsLikedByCurrentUser: isLiked,
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