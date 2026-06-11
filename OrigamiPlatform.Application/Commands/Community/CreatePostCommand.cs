namespace OrigamiPlatform.Application.Commands.Community;

public record CreatePostCommand(CreatePostRequest Request, Guid AuthorId) : IRequest<ApiResponse<Guid>>;