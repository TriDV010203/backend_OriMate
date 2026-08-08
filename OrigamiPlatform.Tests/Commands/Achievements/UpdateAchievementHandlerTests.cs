using Moq;
using OrigamiPlatform.Application.Commands.Achievements;
using OrigamiPlatform.Application.DTOs.Achievements;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Achievements;

public class UpdateAchievementHandlerTests
{
    private readonly Mock<IAchievementRepository> _achievementRepoMock = new();

    private UpdateAchievementHandler CreateHandler()
        => new(_achievementRepoMock.Object);

    private static Achievement BuildAchievement(Guid userId)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TutorialId = Guid.NewGuid(),
            Note = "Old Note",
            PhotoUrl = "Old PhotoUrl",
            IsPublic = false,
            CreatedAt = DateTime.UtcNow,
            Tutorial = new Tutorial { Id = Guid.NewGuid(), Title = "Tutorial", Slug = "tutorial" }
        };

    [Fact]
    public async Task HandleAsync_ValidRequest_UpdatesAchievementAndReturnsDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var achievement = BuildAchievement(userId);
        var request = new UpdateAchievementRequest("New PhotoUrl", "New Note", true);
        var command = new UpdateAchievementCommand(userId, achievement.Id, request);

        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievement.Id, default))
            .ReturnsAsync(achievement);

        _achievementRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Achievement>(), default))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.Equal("New PhotoUrl", achievement.PhotoUrl);
        Assert.Equal("New Note", achievement.Note);
        Assert.True(achievement.IsPublic);
        Assert.NotEqual(default, achievement.UpdatedAt);

        Assert.Equal(achievement.Id, result.Id);
        Assert.Equal("New PhotoUrl", result.PhotoUrl);
        Assert.Equal("New Note", result.Note);

        _achievementRepoMock.Verify(r => r.UpdateAsync(achievement, default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AchievementNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var achievementId = Guid.NewGuid();
        var command = new UpdateAchievementCommand(userId, achievementId, new UpdateAchievementRequest(null, null, true));

        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievementId, default))
            .ReturnsAsync((Achievement?)null);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command));
        Assert.Equal("Achievement not found.", ex.Message);
        
        _achievementRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Achievement>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_UserNotOwner_ThrowsForbiddenException()
    {
        // Arrange
        var requestUserId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var achievement = BuildAchievement(ownerUserId);
        var command = new UpdateAchievementCommand(requestUserId, achievement.Id, new UpdateAchievementRequest(null, null, true));

        _achievementRepoMock.Setup(r => r.GetByIdAsync(achievement.Id, default))
            .ReturnsAsync(achievement);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => handler.HandleAsync(command));
        Assert.Equal("You can only update your own achievement.", ex.Message);
        
        _achievementRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Achievement>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_PhotoUrlTooLong_ThrowsDomainException()
    {
        // Arrange
        var tooLongPhotoUrl = new string('a', 513);
        var command = new UpdateAchievementCommand(Guid.NewGuid(), Guid.NewGuid(), new UpdateAchievementRequest(tooLongPhotoUrl, null, true));
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(command));
        Assert.Equal("Photo URL must not exceed 512 characters.", ex.Message);
        
        _achievementRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NoteTooLong_ThrowsDomainException()
    {
        // Arrange
        var tooLongNote = new string('a', 501);
        var command = new UpdateAchievementCommand(Guid.NewGuid(), Guid.NewGuid(), new UpdateAchievementRequest(null, tooLongNote, true));
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(command));
        Assert.Equal("Note must not exceed 500 characters.", ex.Message);
        
        _achievementRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), default), Times.Never);
    }
}
