using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.Wishlists;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Application.DTOs.CommunityPosts;

namespace OrigamiPlatform.Application.Queries.Wishlists;

public class GetWishlistHandler
{
    private readonly IWishlistRepository _wishlists;
    private readonly ITutorialRepository _tutorials;
    private readonly ICommunityPostRepository _posts;

    public GetWishlistHandler(
        IWishlistRepository wishlists,
        ITutorialRepository tutorials,
        ICommunityPostRepository posts)
    {
        _wishlists = wishlists;
        _tutorials = tutorials;
        _posts = posts;
    }

    public async Task<PagedResult<WishlistDto>> HandleAsync(GetWishlistQuery query, CancellationToken ct = default)
    {
        var pagedData = await _wishlists.GetUserWishlistAsync(query.UserId, query.Type, query.Page, query.PageSize, ct);

        var tutorialIds = pagedData.Items.Where(w => w.TargetType == TargetType.Tutorial).Select(w => w.TargetId).ToList();
        var postIds = pagedData.Items.Where(w => w.TargetType == TargetType.CommunityPost).Select(w => w.TargetId).ToList();

        var tutorials = tutorialIds.Any() ? await _tutorials.GetByIdsAsync(tutorialIds, ct) : [];
        var posts = postIds.Any() ? await _posts.GetByIdsAsync(postIds, ct) : [];

        var dtos = pagedData.Items.Select(w => {
            WishlistTutorialDto? tutorialDto = null;
            WishlistPostDto? postDto = null;

            if (w.TargetType == TargetType.Tutorial)
            {
                var tut = tutorials.FirstOrDefault(t => t.Id == w.TargetId);
                if (tut != null)
                {
                    tutorialDto = new WishlistTutorialDto(
                        Title: tut.Title,
                        CoverImageUrl: tut.CoverImageUrl ?? string.Empty,
                        Description: tut.Description
                    );
                }
            }
            else if (w.TargetType == TargetType.CommunityPost)
            {
                var post = posts.FirstOrDefault(p => p.Id == w.TargetId);
                if (post != null)
                {
                    postDto = new WishlistPostDto(

                        Content: post.Content,
                        Media: post.Media?.Select(m => new MediaItemDto
                        {
                            MediaUrl = m.Url,
                            MediaType = m.MediaType
                        }).ToList() ?? []
                    );
                }
            }

            return new WishlistDto(
                TargetId: w.TargetId,
                TargetType: w.TargetType,
                CreatedAt: w.CreatedAt,
                Tutorial: tutorialDto,
                CommunityPost: postDto
            );
        }).ToList();

        return new PagedResult<WishlistDto>(dtos, pagedData.TotalCount, query.Page, query.PageSize, pagedData.TotalPages);
    }
}