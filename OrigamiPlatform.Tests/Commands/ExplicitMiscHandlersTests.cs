using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.AdminConfiguration;
using OrigamiPlatform.Application.Queries.Subscriptions;
using OrigamiPlatform.Application.Queries.CommunityPosts;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;

namespace OrigamiPlatform.Tests.Commands;

public class ExplicitMiscHandlersTests
{
    [Fact]
    public async Task AssignRoleHandler_ValidData_AssignsRole()
    {
        var users = new Mock<IUserRepository>();
        var audit = new Mock<IAuditLogRepository>();
        
        var handler = new AssignRoleHandler(users.Object, audit.Object);
        var request = new AssignRoleRequest("Manager");
        var userId = Guid.NewGuid();
        
        var user = new User { Id = userId, Roles = new List<UserRole>() };
        users.Setup(x => x.GetByIdAsync(userId, default)).ReturnsAsync(user);
        
        await handler.HandleAsync(new AssignRoleCommand(Guid.NewGuid(), userId, request));
        
        users.Verify(x => x.AddRoleAsync(It.IsAny<UserRole>(), default), Times.Once);
        audit.Verify(x => x.LogAsync(It.IsAny<AuditLog>(), default), Times.Once);
    }
    
    [Fact]
    public async Task CreateBlockedWordHandler_ValidData_CreatesWord()
    {
        var words = new Mock<IBlockedWordRepository>();
        var svc = new Mock<IBlockedWordService>();
        var audit = new Mock<IAuditLogRepository>();
        
        var handler = new CreateBlockedWordHandler(words.Object, svc.Object, audit.Object);
        var request = new CreateBlockedWordRequest("badword");
        
        words.Setup(x => x.ExistsByWordAsync("badword", default)).ReturnsAsync(false);
        words.Setup(x => x.AddAsync(It.IsAny<BlockedWord>(), default)).ReturnsAsync(new BlockedWord { Id = 1, Word = "badword" });
        
        var result = await handler.HandleAsync(new CreateBlockedWordCommand(Guid.NewGuid(), request));
        
        Assert.NotNull(result);
        Assert.Equal("badword", result.Word);
    }
    
    [Fact]
    public async Task GetCreatorRevenueHandler_ValidData_ReturnsRevenue()
    {
        var subs = new Mock<IVipSubscriptionRepository>();
        var trans = new Mock<ITransactionRepository>();
        
        var handler = new GetCreatorRevenueHandler(subs.Object, trans.Object);
        var creatorId = Guid.NewGuid();
        
        subs.Setup(x => x.CountActiveSubscribersAsync(creatorId, default)).ReturnsAsync(5);
        trans.Setup(x => x.CountPendingByCreatorAsync(creatorId, default)).ReturnsAsync(2);
        trans.Setup(x => x.GetConfirmedRevenueAsync(creatorId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default)).ReturnsAsync(100);
        trans.Setup(x => x.GetTotalConfirmedRevenueAsync(creatorId, default)).ReturnsAsync(500);
        
        var subscribers = new List<VipSubscription>
        {
            new VipSubscription { Subscriber = new User() }
        };
        subs.Setup(x => x.GetActiveSubscribersByCreatorAsync(creatorId, default)).ReturnsAsync(subscribers);
        
        var result = await handler.HandleAsync(new GetCreatorRevenueQuery(creatorId, creatorId));
        
        Assert.Equal(5, result.ActiveSubscriberCount);
        Assert.Equal(100, result.NetRevenueThisMonth);
        Assert.Single(result.Subscribers);
    }
    
    [Fact]
    public async Task GetCommunityFeedHandler_ValidData_ReturnsFeed()
    {
        var posts = new Mock<ICommunityPostRepository>();
        var likes = new Mock<ILikeRepository>();
        var follows = new Mock<IFollowRepository>();
        var comments = new Mock<ICommentRepository>();
        
        var handler = new GetCommunityFeedHandler(posts.Object, likes.Object, follows.Object, comments.Object);
        var currentUserId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        
        follows.Setup(x => x.GetFollowingIdsAsync(currentUserId, default)).ReturnsAsync(new List<Guid> { authorId });
        
        var postItems = new List<CommunityPost>
        {
            new CommunityPost { Id = Guid.NewGuid(), AuthorId = authorId, Content = "Hello", Media = new List<CommunityPostMedia> { new CommunityPostMedia { Url = "test" } } }
        };
        posts.Setup(x => x.GetCommunityFeedAsync(It.IsAny<List<Guid>>(), 0, 10)).ReturnsAsync(postItems);
        
        likes.Setup(x => x.GetLikeCountAsync(It.IsAny<Guid>(), TargetType.CommunityPost)).ReturnsAsync(10);
        comments.Setup(x => x.GetCommentCountAsync(It.IsAny<Guid>(), TargetType.CommunityPost, default)).ReturnsAsync(5);
        
        var result = await handler.HandleAsync(new GetCommunityFeedQuery(currentUserId, 1, 10));
        
        Assert.Single(result);
        Assert.Equal(10, result[0].LikeCount);
        Assert.Equal(5, result[0].CommentCount);
        Assert.True(result[0].IsFromFollowedCreator);
    }
}
