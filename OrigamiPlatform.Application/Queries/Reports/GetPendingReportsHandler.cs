using OrigamiPlatform.Application.DTOs.Reports;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Application.Queries.Reports;

public class GetPendingReportsHandler
{
    private readonly IReportRepository _reports;
    private readonly ICommunityPostRepository _posts;
    private readonly ICommentRepository _comments;

    // Inject thêm 2 Repositories để gọi nội dung
    public GetPendingReportsHandler(
        IReportRepository reports,
        ICommunityPostRepository posts,
        ICommentRepository comments)
    {
        _reports = reports;
        _posts = posts;
        _comments = comments;
    }

    public async Task<List<PendingReportDto>> HandleAsync(GetPendingReportsQuery query, CancellationToken ct = default)
    {
        var skip = (query.Page - 1) * query.PageSize;

        // 1. Lấy danh sách Report chờ xử lý từ bảng Reports
        var reports = await _reports.GetPendingReportsAsync(skip, query.PageSize);
        var resultList = new List<PendingReportDto>();

        // 2. Vòng lặp để đi nhặt nội dung cho từng Report
        foreach (var r in reports)
        {
            string? targetContent = null;

            // Nếu người ta report Bài viết -> Lội vào bảng Posts lấy Content
            if (r.TargetType == TargetType.CommunityPost)
            {
                var post = await _posts.GetByIdAsync(r.TargetId);
                targetContent = post?.Content;
            }
            // Nếu người ta report Bình luận -> Lội vào bảng Comments lấy Content
            else if (r.TargetType == TargetType.Comment)
            {
                var comment = await _comments.GetByIdAsync(r.TargetId);
                targetContent = comment?.Content;
            }

            // Gói ghém lại thành DTO trả về cho FE
            resultList.Add(new PendingReportDto(
                Id: r.Id,
                ReporterId: r.ReporterId,
                TargetType: r.TargetType,
                TargetId: r.TargetId,
                Reason: r.Reason,
                CreatedAt: r.CreatedAt,
                TargetContent: targetContent // Gắn nội dung thực tế vào đây!
            ));
        }

        return resultList;
    }
}