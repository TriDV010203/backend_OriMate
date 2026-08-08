using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Comments;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Comments;

public class AddCommentHandlerTests
{
    private readonly Mock<ICommentRepository> _mockComments;
    private readonly Mock<IBlockedWordService> _mockBlockedWords;
    private readonly Mock<INotificationService> _mockNotifications;
    private readonly Mock<ICommunityPostRepository> _mockPosts;
    private readonly Mock<ITutorialRepository> _mockTutorials;
    private readonly Mock<IStuckThreadRepository> _mockStuckThreads;
    private readonly AddCommentHandler _handler;

    public AddCommentHandlerTests()
    {
        _mockComments = new Mock<ICommentRepository>();
        _mockBlockedWords = new Mock<IBlockedWordService>();
        _mockNotifications = new Mock<INotificationService>();
        _mockPosts = new Mock<ICommunityPostRepository>();
        _mockTutorials = new Mock<ITutorialRepository>();
        _mockStuckThreads = new Mock<IStuckThreadRepository>();

        _handler = new AddCommentHandler(
            _mockComments.Object,
            _mockBlockedWords.Object,
            _mockNotifications.Object,
            _mockPosts.Object,
            _mockTutorials.Object,
            _mockStuckThreads.Object
        );
    }

    [Fact]
    public async Task HandleAsync_ContentEmpty_ThrowsDomainException()
    {
        var command = new AddCommentCommand(Guid.NewGuid(), Guid.NewGuid(), TargetType.CommunityPost, "");

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("between 1 and 500 characters", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ContentTooLong_ThrowsDomainException()
    {
        var command = new AddCommentCommand(Guid.NewGuid(), Guid.NewGuid(), TargetType.CommunityPost, new string('a', 501));

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("between 1 and 500 characters", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ContentContainsBlockedWord_ThrowsDomainException()
    {
        var command = new AddCommentCommand(Guid.NewGuid(), Guid.NewGuid(), TargetType.CommunityPost, "Bad comment");
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Bad comment", default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("blocked words", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidComment_CommunityPost_SendsNotification()
    {
        var postAuthorId = Guid.NewGuid();
        var command = new AddCommentCommand(Guid.NewGuid(), Guid.NewGuid(), TargetType.CommunityPost, "Valid comment");
        
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockPosts.Setup(p => p.GetByIdAsync(command.TargetId)).ReturnsAsync(new CommunityPost { AuthorId = postAuthorId });

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockComments.Verify(c => c.AddAsync(It.Is<Comment>(x => x.Content == "Valid comment" && x.TargetId == command.TargetId)), Times.Once);
        _mockNotifications.Verify(n => n.NotifyUserAsync(postAuthorId, NotificationType.System, "Bài viết của bạn có bình luận mới.", "CommunityPost", command.TargetId, default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ValidComment_Tutorial_SendsNotification()
    {
        var tutorialAuthorId = Guid.NewGuid();
        var command = new AddCommentCommand(Guid.NewGuid(), Guid.NewGuid(), TargetType.Tutorial, "Valid comment");
        
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockTutorials.Setup(t => t.GetByIdWithStepsAsync(command.TargetId, default)).ReturnsAsync(new Tutorial { AuthorId = tutorialAuthorId });

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockNotifications.Verify(n => n.NotifyUserAsync(tutorialAuthorId, NotificationType.System, "Bài viết của bạn có bình luận mới.", "Tutorial", command.TargetId, default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ValidComment_StuckThread_SendsNotification()
    {
        var threadAuthorId = Guid.NewGuid();
        var command = new AddCommentCommand(Guid.NewGuid(), Guid.NewGuid(), TargetType.StuckThread, "Valid comment");
        
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockStuckThreads.Setup(t => t.GetByIdAsync(command.TargetId, default)).ReturnsAsync(new StuckThread { UserId = threadAuthorId });

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockNotifications.Verify(n => n.NotifyUserAsync(threadAuthorId, NotificationType.System, "Bài viết của bạn có bình luận mới.", "StuckThread", command.TargetId, default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ValidComment_AuthorIsSelf_NoNotification()
    {
        var authorId = Guid.NewGuid();
        var command = new AddCommentCommand(authorId, Guid.NewGuid(), TargetType.CommunityPost, "Valid comment");
        
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockPosts.Setup(p => p.GetByIdAsync(command.TargetId)).ReturnsAsync(new CommunityPost { AuthorId = authorId });

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockNotifications.Verify(n => n.NotifyUserAsync(It.IsAny<Guid>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NotificationFails_DoesNotThrow()
    {
        var postAuthorId = Guid.NewGuid();
        var command = new AddCommentCommand(Guid.NewGuid(), Guid.NewGuid(), TargetType.CommunityPost, "Valid comment");
        
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockPosts.Setup(p => p.GetByIdAsync(command.TargetId)).ReturnsAsync(new CommunityPost { AuthorId = postAuthorId });
        _mockNotifications.Setup(n => n.NotifyUserAsync(It.IsAny<Guid>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), default)).ThrowsAsync(new Exception("Notification failed"));

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockComments.Verify(c => c.AddAsync(It.IsAny<Comment>()), Times.Once);
    }
}
