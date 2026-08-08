using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using OrigamiPlatform.Application.Queries.Reports;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Tests.Queries.Reports;

public class ExplicitGetPendingReportsHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsMappedReports_WithContent()
    {
        var reports = new Mock<IReportRepository>();
        var posts = new Mock<ICommunityPostRepository>();
        var comments = new Mock<ICommentRepository>();
        var tutorials = new Mock<ITutorialRepository>();

        var handler = new GetPendingReportsHandler(reports.Object, posts.Object, comments.Object, tutorials.Object);
        
        var postTargetId = Guid.NewGuid();
        var commentTargetId = Guid.NewGuid();
        var tutorialTargetId = Guid.NewGuid();

        var pendingReports = new List<Report>
        {
            new Report { Id = Guid.NewGuid(), TargetType = TargetType.CommunityPost, TargetId = postTargetId, Reason = "Spam" },
            new Report { Id = Guid.NewGuid(), TargetType = TargetType.Comment, TargetId = commentTargetId, Reason = "Abuse" },
            new Report { Id = Guid.NewGuid(), TargetType = TargetType.Tutorial, TargetId = tutorialTargetId, Reason = "Inappropriate" }
        };

        reports.Setup(x => x.GetPendingReportsAsync(0, 10)).ReturnsAsync(pendingReports);

        posts.Setup(x => x.GetByIdAsync(postTargetId)).ReturnsAsync(new CommunityPost { Id = postTargetId, Content = "Bad Post" });
        comments.Setup(x => x.GetByIdAsync(commentTargetId)).ReturnsAsync(new Comment { Id = commentTargetId, Content = "Bad Comment" });
        tutorials.Setup(x => x.GetByIdWithStepsAsync(tutorialTargetId, default)).ReturnsAsync(new Tutorial { Id = tutorialTargetId, Title = "Bad Tutorial" });

        var query = new GetPendingReportsQuery(1, 10);
        var result = await handler.HandleAsync(query);

        Assert.Equal(3, result.Count);
        Assert.Equal("Bad Post", result.ElementAt(0).TargetContent);
        Assert.Equal("Bad Comment", result[1].TargetContent);
        Assert.Equal("Bad Tutorial", result[2].TargetContent);
    }
}
