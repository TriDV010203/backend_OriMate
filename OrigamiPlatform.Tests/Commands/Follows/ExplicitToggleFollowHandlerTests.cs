using Moq;
using OrigamiPlatform.Application.Commands.Follows;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.Tests.Commands.Follows;

public class ExplicitToggleFollowHandlerTests
{
    private readonly Mock<IFollowRepository> _mockFollows;
    private readonly Mock<INotificationService> _mockNotifications;
    private readonly ToggleFollowHandler _handler;

    public ExplicitToggleFollowHandlerTests()
    {
        _mockFollows = new Mock<IFollowRepository>();
        _mockNotifications = new Mock<INotificationService>();
        _handler = new ToggleFollowHandler(_mockFollows.Object, _mockNotifications.Object);
    }

    [Fact]
    public async Task HandleAsync_NotFollowing_AddsFollowAndNotifies_ReturnsTrue()
    {
        // Arrange
        var followerId = Guid.NewGuid();
        var followingId = Guid.NewGuid();
        var command = new ToggleFollowCommand(followerId, followingId);

        _mockFollows
            .Setup(x => x.GetFollowAsync(followerId, followingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FollowRelationship?)null);

        _mockFollows
            .Setup(x => x.AddAsync(It.IsAny<FollowRelationship>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockNotifications
            .Setup(x => x.NotifyUserAsync(
                followingId, 
                NotificationType.System, 
                It.IsAny<string>(), 
                "User", 
                followerId, 
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result);
        
        _mockFollows.Verify(x => x.AddAsync(It.Is<FollowRelationship>(f => 
            f.FollowerId == followerId && 
            f.FollowingId == followingId
        ), It.IsAny<CancellationToken>()), Times.Once);

        _mockNotifications.Verify(x => x.NotifyUserAsync(
            followingId, 
            NotificationType.System, 
            "Bạn có người theo dõi mới.", 
            "User", 
            followerId, 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AlreadyFollowing_RemovesFollow_ReturnsFalse()
    {
        // Arrange
        var followerId = Guid.NewGuid();
        var followingId = Guid.NewGuid();
        var command = new ToggleFollowCommand(followerId, followingId);
        var existingFollow = new FollowRelationship 
        { 
            FollowerId = followerId, 
            FollowingId = followingId,
            CreatedAt = DateTime.UtcNow
        };

        _mockFollows
            .Setup(x => x.GetFollowAsync(followerId, followingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFollow);

        _mockFollows
            .Setup(x => x.RemoveAsync(existingFollow, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result);
        _mockFollows.Verify(x => x.RemoveAsync(existingFollow, It.IsAny<CancellationToken>()), Times.Once);
        
        _mockNotifications.Verify(x => x.NotifyUserAsync(
            It.IsAny<Guid>(), 
            It.IsAny<NotificationType>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<Guid>(), 
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
