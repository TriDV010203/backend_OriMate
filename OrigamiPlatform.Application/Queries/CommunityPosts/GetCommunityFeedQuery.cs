namespace OrigamiPlatform.Application.Queries.CommunityPosts;
public record GetCommunityFeedQuery(Guid? CurrentUserId, int Page = 1, int PageSize = 20);