using Moq;
using OrigamiPlatform.Application.Commands.Achievements;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.Achievements;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Achievements;

public class CreateAchievementHandlerTests
{
    private readonly Mock<IAchievementRepository> _achievements = new();
    private readonly Mock<IPersonalMilestoneRepository> _milestones = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<IHatGapTransactionRepository> _hatGapTransactions = new();
    private readonly Mock<ILearningPathRepository> _learningPaths = new();
    private readonly Mock<ILearningPathCompletionRepository> _pathCompletions = new();
    private readonly Mock<IBadgeRepository> _badges = new();
    private readonly Mock<IUserBadgeRepository> _userBadges = new();

    // HatGapAwardService/BadgeAwardService are concrete, non-virtual classes (Moq can't stub them),
    // so the handler is exercised against real instances backed by mocked repositories instead.
    private CreateAchievementHandler CreateHandler()
        => new(
            _achievements.Object,
            _milestones.Object,
            _notifications.Object,
            new HatGapAwardService(_hatGapTransactions.Object),
            _learningPaths.Object,
            _pathCompletions.Object,
            new BadgeAwardService(_badges.Object, _userBadges.Object, _notifications.Object));

    private static Achievement BuildAchievement(Guid userId, Guid tutorialId, string? note = null, string? photoUrl = null)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TutorialId = tutorialId,
            Note = note,
            PhotoUrl = photoUrl,
            IsPublic = true,
            CreatedAt = DateTime.UtcNow,
            Tutorial = new Tutorial { Id = tutorialId, Title = "Crane", Slug = "crane" }
        };

    [Fact]
    public async Task HandleAsync_ValidRequest_CreatesAchievementAndReturnsDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tutorialId = Guid.NewGuid();
        var request = new CreateAchievementRequest(tutorialId, "https://photo.example/1.jpg", "Great fold", true);
        var command = new CreateAchievementCommand(userId, request);

        _achievements.Setup(r => r.PublishedTutorialExistsAsync(tutorialId, default))
            .ReturnsAsync(true);
        _achievements.Setup(r => r.GetByUserAndTutorialAsync(userId, tutorialId, default))
            .ReturnsAsync((Achievement?)null);

        Achievement? added = null;
        _achievements.Setup(r => r.AddAsync(It.IsAny<Achievement>(), default))
            .Callback<Achievement, CancellationToken>((a, _) => added = a)
            .Returns(Task.CompletedTask);

        _achievements.Setup(r => r.CountByUserAsync(userId, default))
            .ReturnsAsync(1);

        _achievements.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(() => added is null ? null : BuildAchievement(userId, tutorialId, added.Note, added.PhotoUrl));

        var handler = CreateHandler();

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(userId, result.UserId);
        Assert.Equal(tutorialId, result.TutorialId);
        Assert.Equal("Great fold", result.Note);
        _achievements.Verify(r => r.AddAsync(It.IsAny<Achievement>(), default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_TutorialNotPublished_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tutorialId = Guid.NewGuid();
        var request = new CreateAchievementRequest(tutorialId, null, null, true);
        var command = new CreateAchievementCommand(userId, request);

        _achievements.Setup(r => r.PublishedTutorialExistsAsync(tutorialId, default))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command));
        Assert.Equal("Published tutorial not found.", ex.Message);
        _achievements.Verify(r => r.AddAsync(It.IsAny<Achievement>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_DuplicateAchievement_ThrowsDomainException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tutorialId = Guid.NewGuid();
        var request = new CreateAchievementRequest(tutorialId, null, null, true);
        var command = new CreateAchievementCommand(userId, request);

        _achievements.Setup(r => r.PublishedTutorialExistsAsync(tutorialId, default))
            .ReturnsAsync(true);
        _achievements.Setup(r => r.GetByUserAndTutorialAsync(userId, tutorialId, default))
            .ReturnsAsync(BuildAchievement(userId, tutorialId));

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(command));
        Assert.Equal("You already marked this tutorial as completed.", ex.Message);
        _achievements.Verify(r => r.AddAsync(It.IsAny<Achievement>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NoteExceedsMaxLength_ThrowsDomainException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tutorialId = Guid.NewGuid();
        var tooLongNote = new string('a', 501); // BV-01: max is 500
        var request = new CreateAchievementRequest(tutorialId, null, tooLongNote, true);
        var command = new CreateAchievementCommand(userId, request);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(command));
        _achievements.Verify(r => r.PublishedTutorialExistsAsync(It.IsAny<Guid>(), default), Times.Never);
        _achievements.Verify(r => r.AddAsync(It.IsAny<Achievement>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_TenthAchievement_UnlocksMilestone()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tutorialId = Guid.NewGuid();
        var request = new CreateAchievementRequest(tutorialId, null, null, true);
        var command = new CreateAchievementCommand(userId, request);

        _achievements.Setup(r => r.PublishedTutorialExistsAsync(tutorialId, default))
            .ReturnsAsync(true);
        _achievements.Setup(r => r.GetByUserAndTutorialAsync(userId, tutorialId, default))
            .ReturnsAsync((Achievement?)null);
        _achievements.Setup(r => r.AddAsync(It.IsAny<Achievement>(), default))
            .Returns(Task.CompletedTask);
        _achievements.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(BuildAchievement(userId, tutorialId));

        // 10th achievement reaches the first milestone threshold
        _achievements.Setup(r => r.CountByUserAsync(userId, default))
            .ReturnsAsync(10);
        _milestones.Setup(m => m.ExistsAsync(userId, 10, default))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        _milestones.Verify(m => m.AddAsync(
            It.Is<PersonalMilestone>(p => p.UserId == userId && p.Threshold == 10),
            default),
            Times.Once);
        _notifications.Verify(n => n.NotifyUserAsync(
            userId,
            It.IsAny<OrigamiPlatform.Domain.Enums.NotificationType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            default),
            Times.Once);
    }
}
