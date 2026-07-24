
using OrigamiPlatform.Application.DTOs.CommunityPosts;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Queries.CommunityPosts;

public class GetCommunityPostByIdHandler
{
    private readonly ICommunityPostRepository _posts;
    private readonly ILikeRepository _likes;
    private readonly ICommentRepository _comments;

    public GetCommunityPostByIdHandler(
        ICommunityPostRepository posts,
        ILikeRepository likes,
        ICommentRepository comments)
    {
        _posts = posts;
        _likes = likes;
        _comments = comments;
    }

    public async Task<CommunityPostDto?> HandleAsync(GetCommunityPostByIdQuery query, CancellationToken ct = default)
    {
        var post = await _posts.GetByIdAsync(query.PostId);
        if (post == null || post.IsDeleted || !post.IsVisible)
        {
            return null;
        }

        var likeCount = await _likes.GetLikeCountAsync(post.Id, TargetType.CommunityPost);
        var commentCount = await _comments.GetCommentCountAsync(post.Id, TargetType.CommunityPost, ct);

        bool isLiked = false;
        if (query.CurrentUserId.HasValue)
        {
            var likeRecord = await _likes.GetLikeAsync(query.CurrentUserId.Value, post.Id, TargetType.CommunityPost);
            isLiked = likeRecord != null;
        }

        return new CommunityPostDto(
            Id: post.Id,
            AuthorId: post.AuthorId,
            Content: post.Content,
            CreatedAt: post.CreatedAt,
            CommentCount: commentCount,
            LikeCount: likeCount,
            IsLikedByCurrentUser: isLiked,
            IsFromFollowedCreator: false,
            Media: post.Media.OrderBy(m => m.DisplayOrder).Select(m => new MediaItemDto
            {
                MediaUrl = m.Url,
                MediaType = m.MediaType
            }).ToList()
        );
    }
}
