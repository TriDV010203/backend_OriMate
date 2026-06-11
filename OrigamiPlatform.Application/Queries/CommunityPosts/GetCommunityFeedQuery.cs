namespace OrigamiPlatform.Application.Queries.CommunityPosts;

// Lấy tham số CurrentUserId để biết user hiện tại có đang like bài viết không
public record GetCommunityFeedQuery(Guid? CurrentUserId, int Page = 1, int PageSize = 20);