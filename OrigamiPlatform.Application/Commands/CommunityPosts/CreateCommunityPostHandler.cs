using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.CommunityPosts;

public class CreateCommunityPostHandler
{
    private readonly ICommunityPostRepository _posts;
    private readonly IBlockedWordRepository _blockedWords;

    public CreateCommunityPostHandler(ICommunityPostRepository posts, IBlockedWordRepository blockedWords)
        => (_posts, _blockedWords) = (posts, blockedWords);

    public async Task<Guid> HandleAsync(CreateCommunityPostCommand cmd, CancellationToken ct = default)
    {
        // 1. Kiểm tra Boundary Values (BV-15) - Độ dài Text
        if (string.IsNullOrWhiteSpace(cmd.Content) || cmd.Content.Length > 1000)
        {
            throw new DomainException("Post content must be between 1 and 1,000 characters.");
        }

        // 2. Kiểm tra Boundary Values (BV-16) - Số lượng Media
        if (cmd.MediaItems != null && cmd.MediaItems.Count > 10)
        {
            throw new DomainException("A post can have a maximum of 10 media items.");
        }

        // 3. Kiểm tra Blocked Words (NAC-01)
        var blockedWords = await _blockedWords.GetAllBlockedWordsAsync();
        var lowerContent = cmd.Content.ToLowerInvariant();

        foreach (var word in blockedWords)
        {
            if (lowerContent.Contains(word.ToLowerInvariant()))
            {
                throw new DomainException("Your post contains blocked words and cannot be published.");
            }
        }

        // 4. Map Command sang Entity dựa trên cấu trúc thực tế
        var postId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var post = new CommunityPost
        {
            Id = postId,
            AuthorId = cmd.UserId,
            LinkedTutorialId = cmd.TutorialId,
            Content = cmd.Content,
            IsVisible = true,
            IsDeleted = false,
            CreatedAt = now
        };

        // Khởi tạo danh sách Media nếu có
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