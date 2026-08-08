using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Likes;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Tests.Commands.Likes;

public class ToggleLikeHandlerTests
{
    private readonly Mock<ILikeRepository> _mockLikes;
    private readonly ToggleLikeHandler _handler;

    public ToggleLikeHandlerTests()
    {
        _mockLikes = new Mock<ILikeRepository>();
        _handler = new ToggleLikeHandler(_mockLikes.Object);
    }

    [Fact]
    public async Task HandleAsync_LikeExists_RemovesLikeAndReturnsFalse()
    {
        var command = new ToggleLikeCommand(Guid.NewGuid(), Guid.NewGuid(), TargetType.CommunityPost);
        var existingLike = new Like { UserId = command.UserId, TargetId = command.TargetId, TargetType = command.TargetType };
        
        _mockLikes.Setup(l => l.GetLikeAsync(command.UserId, command.TargetId, command.TargetType)).ReturnsAsync(existingLike);

        var result = await _handler.HandleAsync(command);

        Assert.False(result);
        _mockLikes.Verify(l => l.RemoveAsync(existingLike), Times.Once);
        _mockLikes.Verify(l => l.AddAsync(It.IsAny<Like>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_LikeDoesNotExist_AddsLikeAndReturnsTrue()
    {
        var command = new ToggleLikeCommand(Guid.NewGuid(), Guid.NewGuid(), TargetType.Tutorial);
        
        _mockLikes.Setup(l => l.GetLikeAsync(command.UserId, command.TargetId, command.TargetType)).ReturnsAsync((Like?)null);

        var result = await _handler.HandleAsync(command);

        Assert.True(result);
        _mockLikes.Verify(l => l.AddAsync(It.Is<Like>(x => 
            x.UserId == command.UserId &&
            x.TargetId == command.TargetId &&
            x.TargetType == command.TargetType
        )), Times.Once);
        _mockLikes.Verify(l => l.RemoveAsync(It.IsAny<Like>()), Times.Never);
    }
}
