using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Reports;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;
using OrigamiPlatform.Application.DTOs.Reports;

namespace OrigamiPlatform.Tests.Commands.Reports;

public class ExplicitHandleReportHandlerTests
{
    [Fact]
    public async Task HandleAsync_RemoveContent_Post_DeletesPost()
    {
        var reports = new Mock<IReportRepository>();
        var posts = new Mock<ICommunityPostRepository>();
        var comments = new Mock<ICommentRepository>();
        var tutorials = new Mock<ITutorialRepository>();
        var users = new Mock<IUserRepository>();
        var audit = new Mock<IAuditLogRepository>();

        var handler = new HandleReportHandler(reports.Object, posts.Object, comments.Object, tutorials.Object, users.Object, audit.Object);
        var reportId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        
        reports.Setup(x => x.GetByIdAsync(reportId)).ReturnsAsync(new Report { Id = reportId, TargetType = TargetType.CommunityPost, TargetId = targetId, Status = ReportStatus.Pending });
        var post = new CommunityPost { Id = targetId, IsDeleted = false };
        posts.Setup(x => x.GetByIdAsync(targetId)).ReturnsAsync(post);

        await handler.HandleAsync(new HandleReportCommand(reportId, reportId, ReportActionType.RemoveContent));

        Assert.True(post.IsDeleted);
        reports.Verify(x => x.UpdateAsync(It.IsAny<Report>()), Times.Once);
        audit.Verify(x => x.LogAsync(It.IsAny<AuditLog>(), default), Times.Once);
    }
    
    [Fact]
    public async Task HandleAsync_SuspendAccount_Tutorial_SuspendsUser()
    {
        var reports = new Mock<IReportRepository>();
        var posts = new Mock<ICommunityPostRepository>();
        var comments = new Mock<ICommentRepository>();
        var tutorials = new Mock<ITutorialRepository>();
        var users = new Mock<IUserRepository>();
        var audit = new Mock<IAuditLogRepository>();

        var handler = new HandleReportHandler(reports.Object, posts.Object, comments.Object, tutorials.Object, users.Object, audit.Object);
        var reportId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        
        reports.Setup(x => x.GetByIdAsync(reportId)).ReturnsAsync(new Report { Id = reportId, TargetType = TargetType.Tutorial, TargetId = targetId, Status = ReportStatus.Pending });
        tutorials.Setup(x => x.GetByIdWithStepsAsync(targetId, default)).ReturnsAsync(new Tutorial { Id = targetId, AuthorId = authorId });
        
        var user = new User { Id = authorId, Roles = new List<UserRole> { new UserRole { Role = UserRoleType.User } } };
        users.Setup(x => x.GetByIdAsync(authorId, default)).ReturnsAsync(user);

        await handler.HandleAsync(new HandleReportCommand(reportId, reportId, ReportActionType.SuspendAccount));

        Assert.Equal(AccountStatus.Suspended, user.Status);
        users.Verify(x => x.UpdateAsync(user, default), Times.Once);
    }
}
