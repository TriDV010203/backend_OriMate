using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.TutorialProgress;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.TutorialProgress;

public class CompleteTutorialStepHandlerTests
{
    private readonly Mock<ITutorialStepProgressRepository> _mockProgress;
    private readonly Mock<IUserRepository> _mockUsers;
    private readonly Mock<IStreakLogRepository> _mockStreakLogs;
    private readonly Mock<IDailyQuestRepository> _mockDailyQuests;
    private readonly Mock<IUserDailyQuestProgressRepository> _mockQuestProgress;
    private readonly Mock<INotificationService> _mockNotifications;
    
    private readonly Mock<IHatGapTransactionRepository> _mockHatGapRepo;
    private readonly Mock<IBadgeRepository> _mockBadgeRepo;
    private readonly Mock<IUserBadgeRepository> _mockUserBadgeRepo;

    private readonly CompleteTutorialStepHandler _handler;

    public CompleteTutorialStepHandlerTests()
    {
        _mockProgress = new Mock<ITutorialStepProgressRepository>();
        _mockUsers = new Mock<IUserRepository>();
        _mockStreakLogs = new Mock<IStreakLogRepository>();
        _mockDailyQuests = new Mock<IDailyQuestRepository>();
        _mockQuestProgress = new Mock<IUserDailyQuestProgressRepository>();
        _mockNotifications = new Mock<INotificationService>();

        _mockHatGapRepo = new Mock<IHatGapTransactionRepository>();
        var hatGapService = new HatGapAwardService(_mockHatGapRepo.Object);

        _mockBadgeRepo = new Mock<IBadgeRepository>();
        _mockUserBadgeRepo = new Mock<IUserBadgeRepository>();
        var badgeService = new BadgeAwardService(_mockBadgeRepo.Object, _mockUserBadgeRepo.Object, _mockNotifications.Object);

        _handler = new CompleteTutorialStepHandler(
            _mockProgress.Object, 
            _mockUsers.Object, 
            _mockStreakLogs.Object,
            _mockDailyQuests.Object, 
            _mockQuestProgress.Object, 
            hatGapService,
            _mockNotifications.Object, 
            badgeService);
    }

    [Fact]
    public async Task HandleAsync_StepNotFound_ThrowsNotFoundException()
    {
        var command = new CompleteTutorialStepCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _mockProgress.Setup(p => p.GetStepWithTutorialAsync(command.StepId, default)).ReturnsAsync((TutorialStep?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Equal("Tutorial step not found.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_WrongTutorial_ThrowsNotFoundException()
    {
        var command = new CompleteTutorialStepCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var step = new TutorialStep { Id = command.StepId, TutorialId = Guid.NewGuid() };
        _mockProgress.Setup(p => p.GetStepWithTutorialAsync(command.StepId, default)).ReturnsAsync(step);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Equal("This step does not belong to the given tutorial.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_TutorialNotPublished_ThrowsDomainException()
    {
        var command = new CompleteTutorialStepCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var step = new TutorialStep 
        { 
            Id = command.StepId, 
            TutorialId = command.TutorialId, 
            Tutorial = new Tutorial { Status = TutorialStatus.Draft }
        };
        _mockProgress.Setup(p => p.GetStepWithTutorialAsync(command.StepId, default)).ReturnsAsync(step);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("You can only track progress on a published tutorial.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_AlreadyCompleted_ThrowsDomainException()
    {
        var command = new CompleteTutorialStepCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var step = new TutorialStep 
        { 
            Id = command.StepId, 
            TutorialId = command.TutorialId, 
            Tutorial = new Tutorial { Status = TutorialStatus.Published }
        };
        _mockProgress.Setup(p => p.GetStepWithTutorialAsync(command.StepId, default)).ReturnsAsync(step);
        _mockProgress.Setup(p => p.ExistsAsync(command.UserId, command.StepId, default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("You have already completed this step.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_CompletesStepAndReturnsDto()
    {
        // Arrange
        var command = new CompleteTutorialStepCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var step = new TutorialStep 
        { 
            Id = command.StepId, 
            TutorialId = command.TutorialId, 
            Tutorial = new Tutorial { Status = TutorialStatus.Published, Difficulty = TutorialDifficulty.Beginner }
        };
        var streakLog = new StreakLog { UserId = command.UserId, CurrentStreak = 1, LastActiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)) };
        var completedIds = new List<Guid> { command.StepId };

        _mockProgress.Setup(p => p.GetStepWithTutorialAsync(command.StepId, default)).ReturnsAsync(step);
        _mockProgress.Setup(p => p.ExistsAsync(command.UserId, command.StepId, default)).ReturnsAsync(false);
        _mockProgress.Setup(p => p.CountStepsAsync(command.TutorialId, default)).ReturnsAsync(3);
        _mockProgress.Setup(p => p.GetCompletedStepIdsAsync(command.UserId, command.TutorialId, default)).ReturnsAsync(completedIds);
        
        // Mock streak log for UpdateStreakAsync so it doesn't fail silently
        _mockStreakLogs.Setup(s => s.GetByUserIdAsync(command.UserId, default)).ReturnsAsync(streakLog);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        _mockProgress.Verify(p => p.AddAsync(It.Is<TutorialStepProgress>(sp => 
            sp.UserId == command.UserId && 
            sp.TutorialId == command.TutorialId &&
            sp.TutorialStepId == command.StepId), default), Times.Once);

        Assert.Equal(command.TutorialId, result.TutorialId);
        Assert.Equal(3, result.TotalSteps);
        Assert.Equal(1, result.CompletedSteps);
    }
}
