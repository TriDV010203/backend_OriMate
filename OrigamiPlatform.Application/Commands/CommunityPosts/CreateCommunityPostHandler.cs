using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.CommunityPosts;

public class CreateCommunityPostHandler
{
    private readonly ICommunityPostRepository _posts;

    public CreateCommunityPostHandler(ICommunityPostRepository posts)
        => _posts = posts;

    public async Task<Guid> HandleAsync(CreateCommunityPostCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Content) || cmd.Content.Length > 1000)
        {
            throw new DomainException("Post content must be between 1 and 1,000 characters.");
        }

        if (cmd.MediaItems != null && cmd.MediaItems.Count > 10)
        {
            throw new DomainException("A post can have a maximum of 10 media items.");
        }

        var postId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var post = new CommunityPost
        {
            Id = postId,
            AuthorId = cmd.UserId,
            Content = cmd.Content,
            IsVisible = true,
            IsDeleted = false,
            CreatedAt = now
        };

        if (cmd.MediaItems != null && cmd.MediaItems.Any())
        {
            var mediaList = new List<CommunityPostMedia>();
            for (int i = 0; i < cmd.MediaItems.Count; i++)
            {
                var mediaItemDto = cmd.MediaItems[i];
                mediaList.Add(new CommunityPostMedia
                {
                    Id = Guid.NewGuid(),
                    PostId = postId,
                    Url = mediaItemDto.MediaUrl,
                    MediaType = mediaItemDto.MediaType,
                    DisplayOrder = i,
                    CreatedAt = now
                });
            }
            post.Media = mediaList;
        }

        await _posts.AddAsync(post);

        return post.Id;
    }
}
