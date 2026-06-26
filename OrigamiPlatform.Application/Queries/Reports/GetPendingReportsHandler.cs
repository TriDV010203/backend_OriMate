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

            if (r.TargetType == TargetType.CommunityPost)
            {
                var post = await _posts.GetByIdAsync(r.TargetId);
                targetContent = post?.Content;
            }
            else if (r.TargetType == TargetType.Comment)
            {
                var comment = await _comments.GetByIdAsync(r.TargetId);
                targetContent = comment?.Content;
            }
            else if (r.TargetType == TargetType.Tutorial)
            {
                var tutorial = await _tutorials.GetByIdWithStepsAsync(r.TargetId);
                targetContent = tutorial?.Title;
            }

            resultList.Add(new PendingReportDto(
                Id: r.Id,
                ReporterId: r.ReporterId,
                TargetType: r.TargetType,
                TargetId: r.TargetId,
                Reason: r.Reason,
                CreatedAt: r.CreatedAt,
                TargetContent: targetContent 
            ));
        }

        return resultList;
    }
}