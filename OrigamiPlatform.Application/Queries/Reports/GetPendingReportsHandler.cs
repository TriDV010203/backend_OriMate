using OrigamiPlatform.Application.DTOs.Reports;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Queries.Reports;

public class GetPendingReportsHandler
{
    private readonly IReportRepository _reports;
    private readonly ICommunityPostRepository _posts;
    private readonly ICommentRepository _comments;
    private readonly ITutorialRepository _tutorials;

    // Inject thêm 2 Repositories để gọi nội dung
    public GetPendingReportsHandler(
        IReportRepository reports,
        ICommunityPostRepository posts,
        ICommentRepository comments,
        ITutorialRepository tutorials)
    {
        _reports = reports;
        _posts = posts;
        _comments = comments;
        _tutorials = tutorials;
    }

    public async Task<List<PendingReportDto>> HandleAsync(GetPendingReportsQuery query, CancellationToken ct = default)
    {
        var skip = (query.Page - 1) * query.PageSize;

        var reports = await _reports.GetPendingReportsAsync(skip, query.PageSize);
        var resultList = new List<PendingReportDto>();

        foreach (var r in reports)
        {
            string? targetContent = null;
            string? targetSlug = null;
            TargetType? rootTargetType = null;
            Guid? rootTargetId = null;

            if (r.TargetType == TargetType.CommunityPost)
            {
                var post = await _posts.GetByIdAsync(r.TargetId);
                targetContent = post?.Content;
            }
            else if (r.TargetType == TargetType.Comment)
            {
                var comment = await _comments.GetByIdAsync(r.TargetId);
                targetContent = comment?.Content;

                if (comment != null)
                {
                    // Comment có thể là reply của 1 comment khác — lần ngược lên nội dung gốc
                    // (bài đăng/hướng dẫn/chủ đề hỏi) để FE biết mở trang nào và cuộn tới đâu.
                    var currentType = comment.TargetType;
                    var currentId = comment.TargetId;
                    var depth = 0;
                    while (currentType == TargetType.Comment && depth < 10)
                    {
                        var parentComment = await _comments.GetByIdAsync(currentId);
                        if (parentComment == null) break;
                        currentType = parentComment.TargetType;
                        currentId = parentComment.TargetId;
                        depth++;
                    }
                    rootTargetType = currentType;
                    rootTargetId = currentId;

                    if (currentType == TargetType.Tutorial)
                    {
                        var parentTutorial = await _tutorials.GetByIdWithStepsAsync(currentId);
                        targetSlug = parentTutorial?.Slug;
                    }
                }
            }
            else if (r.TargetType == TargetType.Tutorial)
            {
                var tutorial = await _tutorials.GetByIdWithStepsAsync(r.TargetId);
                targetContent = tutorial?.Title;
                targetSlug = tutorial?.Slug;
            }

            resultList.Add(new PendingReportDto(
                Id: r.Id,
                ReporterId: r.ReporterId,
                TargetType: r.TargetType,
                TargetId: r.TargetId,
                Reason: r.Reason,
                CreatedAt: r.CreatedAt,
                TargetContent: targetContent,
                TargetSlug: targetSlug,
                RootTargetType: rootTargetType,
                RootTargetId: rootTargetId
            ));
        }

        return resultList;
    }
}