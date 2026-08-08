using Moq;
using OrigamiPlatform.Application.Commands.Achievements;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Achievements;

public class DeleteAchievementHandlerTests
{
    private readonly Mock<IAchievementRepository> _achievementRepoMock = new();

    private DeleteAchievementHandler CreateHandler()
        => new(_achievementRepoMock.Object);

    [Fact]
    public async Task HandleAsync_ValidRequest_DeletesAchievement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var achievementId = Guid.NewGuid();
        var command = new DeleteAchievementCommand(userId, achievementId);

        var existingAchievement = new Achievement
        {
            Id = achievementId,
            UserId = userId
        };

        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievementId, default))
            .ReturnsAsync(existingAchievement);

        _achievementRepoMock.Setup(r => r.DeleteAsync(existingAchievement, default))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        _achievementRepoMock.Verify(r => r.DeleteAsync(existingAchievement, default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AchievementNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var achievementId = Guid.NewGuid();
        var command = new DeleteAchievementCommand(userId, achievementId);

        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievementId, default))
            .ReturnsAsync((Achievement?)null);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command));
        Assert.Equal("Achievement not found.", ex.Message);
        
        _achievementRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Achievement>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_UserNotOwner_ThrowsForbiddenException()
    {
        // Arrange
        var requestUserId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var achievementId = Guid.NewGuid();
        var command = new DeleteAchievementCommand(requestUserId, achievementId);

        var existingAchievement = new Achievement
        {
            Id = achievementId,
            UserId = ownerUserId // Different from requestUserId
        };

        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievementId, default))
            .ReturnsAsync(existingAchievement);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(command));
        Assert.Equal("You can only delete your own achievement.", ex.Message);
        
        _achievementRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Achievement>(), default), Times.Never);
    }
}
