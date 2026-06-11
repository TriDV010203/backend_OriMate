using OrigamiPlatform.Application.Commands.Community;
using OrigamiPlatform.Application.Interfaces.Repositories;

public class CreatePostHandler : IRequestHandler<CreatePostCommand, ApiResponse<Guid>>
{
    private readonly ICommunityRepository _repository;

    public CreatePostHandler(ICommunityRepository repository) => _repository = repository;

    public async Task<ApiResponse<Guid>> Handle(CreatePostCommand command, CancellationToken cancellationToken)
    {
        var post = new CommunityPost
        {
            Id = Guid.NewGuid(),
            AuthorId = command.AuthorId,
            Content = command.Request.Content,
            LinkedTutorialId = command.Request.LinkedTutorialId,
            CreatedAt = DateTime.UtcNow,
            IsVisible = true, // Bài viết mặc định hiển thị
            IsDeleted = false
        };

        // Gán Media
        foreach (var url in command.Request.MediaUrls)
        {
            post.Media.Add(new CommunityPostMedia
            {
                Id = Guid.NewGuid(),
                Url = url,
                CommunityPostId = post.Id
            });
        }

        await _repository.AddPostAsync(post);
        return ApiResponse<Guid>.Success(post.Id, "Post created successfully.");
    }
}