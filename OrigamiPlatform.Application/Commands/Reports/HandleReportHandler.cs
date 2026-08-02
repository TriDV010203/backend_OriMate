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
    private readonly IUserRepository _users;
    private readonly IAuditLogRepository _auditLog;

    public HandleReportHandler(
        IReportRepository reports,
        ICommunityPostRepository posts,
        ICommentRepository comments,
        ITutorialRepository tutorials,
        IUserRepository users,
        IAuditLogRepository auditLog)
    {
        _reports = reports;
        _posts = posts;
        _comments = comments;
        _tutorials = tutorials;
        _users = users;
        _auditLog = auditLog;
    }

    public async Task HandleAsync(HandleReportCommand cmd, CancellationToken ct = default)
    {
        var report = await _reports.GetByIdAsync(cmd.ReportId);
        if (report == null)
            throw new NotFoundException("Report not found.");

        if (report.Status != ReportStatus.Pending)
            throw new DomainException("This report has already been handled.");

        string auditAction = string.Empty;

        switch (cmd.ActionType)
        {
            case ReportActionType.Dismiss:
                report.Status = ReportStatus.Dismissed;
                auditAction = "DismissReport";
                break;

            case ReportActionType.RemoveContent:
                await RemoveContentAsync(report.TargetType, report.TargetId);
                report.Status = ReportStatus.Reviewed;
                auditAction = "RemoveContentViaReport";
                break;

            case ReportActionType.SuspendAccount:
                var authorId = await ResolveTargetAuthorIdAsync(report.TargetType, report.TargetId);
                var author = await _users.GetByIdAsync(authorId, ct)
                    ?? throw new NotFoundException("Reported user not found.");

                if (author.Roles.Any(r => r.Role == UserRoleType.Admin))
                    throw new ForbiddenException("Cannot suspend an Admin account.");

                author.Status = AccountStatus.Suspended;
                author.UpdatedAt = DateTime.UtcNow;
                await _users.UpdateAsync(author, ct);

                report.Status = ReportStatus.Reviewed;
                auditAction = "SuspendAccountViaReport";
                break;
        }

        report.HandledBy = cmd.ManagerId;
        report.HandledAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;

        await _reports.UpdateAsync(report);

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = cmd.ManagerId,
            Action = auditAction,
            EntityType = "Report",
            EntityId = report.Id.ToString(),
            OldValue = report.Reason,
            NewValue = null,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }

    private async Task<Guid> ResolveTargetAuthorIdAsync(TargetType targetType, Guid targetId)
    {
        if (targetType == TargetType.CommunityPost)
        {
            var post = await _posts.GetByIdAsync(targetId);
            if (post == null)
                throw new NotFoundException("Reported post not found.");
            return post.AuthorId;
        }

        if (targetType == TargetType.Comment)
        {
            var comment = await _comments.GetByIdAsync(targetId);
            if (comment == null)
                throw new NotFoundException("Reported comment not found.");
            return comment.AuthorId;
        }

        if (targetType == TargetType.Tutorial)
        {
            var tutorial = await _tutorials.GetByIdWithStepsAsync(targetId);
            if (tutorial == null)
                throw new NotFoundException("Reported tutorial not found.");
            return tutorial.AuthorId;
        }

        throw new NotFoundException("Reported target not found.");
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