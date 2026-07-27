using Moq;
using OrigamiPlatform.Application.Commands.Achievements;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.Achievements;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Tests.Commands.Achievements;

// AwardLearningPathCompletionAsync is a private method of CreateAchievementHandler, so it is
// exercised indirectly through HandleAsync (same approach as CreateAchievementHandlerTests).
//
// NOTE on scope vs. original spec: ILearningPathRepository exposes a single
// GetPublishedPathContainingTutorialAsync(tutorialId) method that already filters to Published
// paths server-side — there is no separate "path exists but not Published" case observable from
// the handler's perspective; both "tutorial not in any path" and "path not published" surface as
// this method returning null. Those two spec scenarios are therefore merged into one test below.
public class AwardLearningPathCompletionTests
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

    private static Achievement BuildAchievement(Guid userId, Guid tutorialId)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TutorialId = tutorialId,
            IsPublic = true,
            CreatedAt = DateTime.UtcNow,
            Tutorial = new Tutorial { Id = tutorialId, Title = "Crane", Slug = "crane" }
        };

    // Arranges the common CreateAchievement happy path: valid, non-duplicate, tutorial exists.
    // CountByUserAsync is fixed at 1 (below every PersonalMilestone threshold) so the unrelated
    // milestone-reward path never fires and doesn't interfere with the Hạt Gấp assertions here.
    private CreateAchievementCommand ArrangeAchievementCreation(Guid userId, Guid tutorialId)
    {
        var request = new CreateAchievementRequest(tutorialId, null, null, true);
        var command = new CreateAchievementCommand(userId, request);

        _achievements.Setup(r => r.PublishedTutorialExistsAsync(tutorialId, default)).ReturnsAsync(true);
        _achievements.Setup(r => r.GetByUserAndTutorialAsync(userId, tutorialId, default)).ReturnsAsync((Achievement?)null);
        _achievements.Setup(r => r.AddAsync(It.IsAny<Achievement>(), default)).Returns(Task.CompletedTask);
        _achievements.Setup(r => r.CountByUserAsync(userId, default)).ReturnsAsync(1);
        _achievements.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(BuildAchievement(userId, tutorialId));

        return command;
    }

    private static LearningPath BuildPublishedPath(Guid pathId, params Guid[] tutorialIds)
        => new()
        {
            Id = pathId,
            Title = "Bird Basics",
            Status = LearningPathStatus.Published,
            Items = tutorialIds.Select(t => new LearningPathItem { TutorialId = t }).ToList()
        };

    [Fact]
    public async Task Achievement_CompletesLastTutorialInPath_AwardsHatGapAndCreatesCompletion()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tutorialId = Guid.NewGuid();
        var otherTutorialId1 = Guid.NewGuid();
        var otherTutorialId2 = Guid.NewGuid();
        var pathId = Guid.NewGuid();

        var command = ArrangeAchievementCreation(userId, tutorialId);

        var path = BuildPublishedPath(pathId, tutorialId, otherTutorialId1, otherTutorialId2);
        _learningPaths.Setup(l => l.GetPublishedPathContainingTutorialAsync(tutorialId, default)).ReturnsAsync(path);
        _pathCompletions.Setup(p => p.ExistsAsync(userId, pathId, default)).ReturnsAsync(false);
        _achievements.Setup(a => a.GetCompletedTutorialIdsAsync(userId, default))
            .ReturnsAsync(new HashSet<Guid> { tutorialId, otherTutorialId1, otherTutorialId2 });

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        _pathCompletions.Verify(p => p.AddAsync(
            It.Is<LearningPathCompletion>(c => c.UserId == userId && c.LearningPathId == pathId),
            default),
            Times.Once);

        _hatGapTransactions.Verify(t => t.AddAsync(
            It.Is<HatGapTransaction>(tx =>
                tx.UserId == userId &&
                tx.Amount == 5 &&
                tx.Type == HatGapTransactionType.Earn &&
                tx.Source == $"LearningPathComplete_{pathId}"),
            default),
            Times.Once);

        _notifications.Verify(n => n.NotifyUserAsync(
            userId,
            NotificationType.LearningPathCompleted,
            It.IsAny<string>(),
            nameof(LearningPath),
            pathId,
            default),
            Times.Once);
    }

    [Fact]
    public async Task Achievement_PathAlreadyCompleted_NoDoubleReward()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tutorialId = Guid.NewGuid();
        var pathId = Guid.NewGuid();

        var command = ArrangeAchievementCreation(userId, tutorialId);

        var path = BuildPublishedPath(pathId, tutorialId);
        _learningPaths.Setup(l => l.GetPublishedPathContainingTutorialAsync(tutorialId, default)).ReturnsAsync(path);
        _pathCompletions.Setup(p => p.ExistsAsync(userId, pathId, default)).ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        _pathCompletions.Verify(p => p.AddAsync(It.IsAny<LearningPathCompletion>(), default), Times.Never);
        _hatGapTransactions.Verify(t => t.AddAsync(It.IsAny<HatGapTransaction>(), default), Times.Never);
    }

    [Fact]
    public async Task Achievement_TutorialNotInAnyPublishedPath_NoPathReward()
    {
        // Arrange: covers both "tutorial isn't in any Learning Path" and "path exists but isn't
        // Published" — GetPublishedPathContainingTutorialAsync returns null for both cases.
        var userId = Guid.NewGuid();
        var tutorialId = Guid.NewGuid();

        var command = ArrangeAchievementCreation(userId, tutorialId);
        _learningPaths.Setup(l => l.GetPublishedPathContainingTutorialAsync(tutorialId, default))
            .ReturnsAsync((LearningPath?)null);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        _pathCompletions.Verify(p => p.AddAsync(It.IsAny<LearningPathCompletion>(), default), Times.Never);
        _hatGapTransactions.Verify(t => t.AddAsync(It.IsAny<HatGapTransaction>(), default), Times.Never);
    }

    [Fact]
    public async Task Achievement_OneOfMultiplePathTutorialsIncomplete_NoPathReward()
    {
        // Arrange: path has 3 tutorials, user has only completed 2 (including the one just achieved)
        var userId = Guid.NewGuid();
        var tutorialId = Guid.NewGuid();
        var otherTutorialId1 = Guid.NewGuid();
        var otherTutorialId2 = Guid.NewGuid(); // still incomplete
        var pathId = Guid.NewGuid();

        var command = ArrangeAchievementCreation(userId, tutorialId);

        var path = BuildPublishedPath(pathId, tutorialId, otherTutorialId1, otherTutorialId2);
        _learningPaths.Setup(l => l.GetPublishedPathContainingTutorialAsync(tutorialId, default)).ReturnsAsync(path);
        _pathCompletions.Setup(p => p.ExistsAsync(userId, pathId, default)).ReturnsAsync(false);
        _achievements.Setup(a => a.GetCompletedTutorialIdsAsync(userId, default))
            .ReturnsAsync(new HashSet<Guid> { tutorialId, otherTutorialId1 });

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        _pathCompletions.Verify(p => p.AddAsync(It.IsAny<LearningPathCompletion>(), default), Times.Never);
        _hatGapTransactions.Verify(t => t.AddAsync(It.IsAny<HatGapTransaction>(), default), Times.Never);
    }
}
