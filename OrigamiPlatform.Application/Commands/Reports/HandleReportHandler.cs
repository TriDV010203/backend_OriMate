using OrigamiPlatform.Application.DTOs.Reports;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Reports;

public class HandleReportHandler
{
    private readonly IReportRepository _reports;
    private readonly ICommunityPostRepository _posts;
    private readonly ICommentRepository _comments;
    private readonly ITutorialRepository _tutorials;

    public HandleReportHandler(
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

    public async Task HandleAsync(HandleReportCommand cmd, CancellationToken ct = default)
    {
        var report = await _reports.GetByIdAsync(cmd.ReportId);
        if (report == null)
            throw new NotFoundException("Report not found.");

        if (report.Status != ReportStatus.Pending)
            throw new DomainException("This report has already been handled.");

        switch (cmd.ActionType)
        {
            case ReportActionType.Dismiss:
                report.Status = ReportStatus.Dismissed;
                break;

            case ReportActionType.RemoveContent:
                await RemoveContentAsync(report.TargetType, report.TargetId);
                report.Status = ReportStatus.Reviewed;
                break;

            case ReportActionType.SuspendAccount:
                report.Status = ReportStatus.Reviewed;
                break;
        }

        report.HandledBy = cmd.ManagerId;
        report.HandledAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;

        await _reports.UpdateAsync(report);
    }

    private async Task RemoveContentAsync(TargetType targetType, Guid targetId)
    {
        if (targetType == TargetType.CommunityPost)
        {
            var post = await _posts.GetByIdAsync(targetId);
            if (post != null)
            {
                post.IsDeleted = true;
            }
        }
        else if (targetType == TargetType.Tutorial)
        {
            var tutorial = await _tutorials.GetByIdWithStepsAsync(targetId);
            if (tutorial != null)
            {
                tutorial.IsDeleted = true;
            }
        }
        else if (targetType == TargetType.Comment)
        {
            var comment = await _comments.GetByIdAsync(targetId);
            if (comment != null)
            {
                comment.IsDeleted = true;
            }
        }
    }
}