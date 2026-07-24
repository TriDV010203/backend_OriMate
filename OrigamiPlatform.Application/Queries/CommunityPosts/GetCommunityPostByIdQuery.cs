namespace OrigamiPlatform.Application.Queries.CommunityPosts;
public record GetCommunityPostByIdQuery(Guid PostId, Guid? CurrentUserId);
