using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Queries.Comments;

public record GetCommentsQuery(Guid TargetId, TargetType TargetType, int Page, int PageSize);