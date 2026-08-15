using Moq;
using OrigamiPlatform.Application.Commands.TutorialProgress;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Tests.Commands.TutorialProgress;

// AwardStreakMilestoneAsync is a private method of CompleteTutorialStepHandler, so it is exercised
// indirectly through HandleAsync. Every scenario here uses a 2-step tutorial where only 1 step is
// already completed, so the (unrelated) full-tutorial-completion reward path never fires and the
// only Hạt Gấp award that can happen is the streak milestone one under test.
public class AwardStreakMilestoneTests
{
    private readonly Mock<ITutorialStepProgressRepository> _progress = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IStreakLogRepository> _streakLogs = new();
    private readonly Mock<IHatGapTransactionRepository> _hatGapTransactions = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly Mock<IBadgeRepository> _badges = new();
    private readonly Mock<IUserBadgeRepository> _userBadges = new();

    // HatGapAwardService/BadgeAwardService are concrete, non-virtual classes (Moq can't stub them),
    // so the handler is exercised against real instances backed by mocked repositories instead.
    private CompleteTutorialStepHandler CreateHandler()
        => new(
            _progress.Object,
            _users.Object,
            _streakLogs.Object,
            new HatGapAwardService(_hatGapTransactions.Object),
            _notifications.Object,
            new BadgeAwardService(_badges.Object, _userBadges.Object, _notifications.Object));

    private static DateOnly TodayGmt7() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

    private void ArrangeCommonFlow(Guid userId, Guid tutorialId, Guid stepId)
    {
        var tutorial = new Tutorial
        {
            Id = tutorialId,
            Title = "Crane",
            Slug = "crane",
            Status = TutorialStatus.Published,
            Difficulty = TutorialDifficulty.Beginner,
            IsDeleted = false
        };
        var step = new TutorialStep { Id = stepId, TutorialId = tutorialId, Tutorial = tutorial };

        _progress.Setup(p => p.GetStepWithTutorialAsync(stepId, default)).ReturnsAsync(step);
        _progress.Setup(p => p.ExistsAsync(userId, stepId, default)).ReturnsAsync(false);
        _progress.Setup(p => p.AddAsync(It.IsAny<TutorialStepProgress>(), default)).Returns(Task.CompletedTask);

        // Total = 2, only this 1 step completed so far -> full-tutorial reward path is not triggered.
        _progress.Setup(p => p.CountStepsAsync(tutorialId, default)).ReturnsAsync(2);
        _progress.Setup(p => p.GetCompletedStepIdsAsync(userId, tutorialId, default))
            .ReturnsAsync(new List<Guid> { stepId });

        _streakLogs.Setup(s => s.UpdateAsync(It.IsAny<StreakLog>(), default)).Returns(Task.CompletedTask);
    }

    private CompleteTutorialStepCommand ArrangeStreak(
        Guid userId, Guid tutorialId, Guid stepId, int currentStreakBeforeStep, int freezeCount = 0)
    {
        ArrangeCommonFlow(userId, tutorialId, stepId);

        var streak = new StreakLog
        {
            UserId = userId,
            CurrentStreak = currentStreakBeforeStep,
            LongestStreak = currentStreakBeforeStep,
            LastActiveDate = TodayGmt7().AddDays(-1),
            FreezeCount = freezeCount
        };
        _streakLogs.Setup(s => s.GetByUserIdAsync(userId, default)).ReturnsAsync(streak);

        return new CompleteTutorialStepCommand(userId, tutorialId, stepId);
    }

    [Fact]
    public async Task AwardStreakMilestone_StreakReaches7FirstTime_Awards5HatGap()
    {
        // Arrange: CurrentStreak=6 + one more consecutive day => 7 (first milestone tier)
        var userId = Guid.NewGuid();
        var command = ArrangeStreak(userId, Guid.NewGuid(), Guid.NewGuid(), currentStreakBeforeStep: 6);
        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        _hatGapTransactions.Verify(t => t.AddAsync(
            It.Is<HatGapTransaction>(tx =>
                tx.UserId == userId &&
                tx.Amount == 5 &&
                tx.Type == HatGapTransactionType.Earn &&
                tx.Source == "StreakMilestone7"),
            default),
            Times.Once);

        _notifications.Verify(n => n.NotifyUserAsync(
            userId,
            NotificationType.StreakMilestoneReached,
            It.IsAny<string>(),
            nameof(StreakLog),
            userId,
            default),
            Times.Once);
    }

    // NOTE: AwardStreakMilestoneAsync (CompleteTutorialStepHandler) has NO idempotency guard —
    // IHatGapTransactionRepository exposes no ExistsBySourceAsync-style lookup. Dedup today relies
    // entirely on CurrentStreak only ever incrementing by 1, so a milestone value is landed on
    // exactly once per streak run. This test documents the desired behavior; it is skipped because
    // asserting it against the current implementation would fail (per user decision, not implemented
    // as a fix here — flagging the gap instead).
    [Fact(Skip = "AwardStreakMilestoneAsync has no dedup guard today (no ExistsBySourceAsync in IHatGapTransactionRepository) — documents desired behavior, not current behavior.")]
    public async Task AwardStreakMilestone_Streak7AlreadyAwarded_NoDuplicateHatGap()
    {
        // Arrange: streak lands on 7 again in a later streak run after already being rewarded once.
        var userId = Guid.NewGuid();
        var command = ArrangeStreak(userId, Guid.NewGuid(), Guid.NewGuid(), currentStreakBeforeStep: 6);
        var handler = CreateHandler();

        // (Would-be) arrange: a prior "StreakMilestone7" transaction already exists for this user.
        // No repository method exists today to express that precondition.

        // Act
        await handler.HandleAsync(command);

        // Assert (desired, not current, behavior)
        _hatGapTransactions.Verify(t => t.AddAsync(
            It.Is<HatGapTransaction>(tx => tx.Source == "StreakMilestone7"), default),
            Times.Never);
    }

    [Fact]
    public async Task AwardStreakMilestone_StreakReaches14_Awards10HatGapAndNotReAwards7()
    {
        // Arrange: CurrentStreak=13 + one more consecutive day => 14
        var userId = Guid.NewGuid();
        var command = ArrangeStreak(userId, Guid.NewGuid(), Guid.NewGuid(), currentStreakBeforeStep: 13);
        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert: only the 14-day milestone fires, never the 7-day one
        _hatGapTransactions.Verify(t => t.AddAsync(
            It.Is<HatGapTransaction>(tx =>
                tx.Amount == 10 &&
                tx.Type == HatGapTransactionType.Earn &&
                tx.Source == "StreakMilestone14"),
            default),
            Times.Once);

        _hatGapTransactions.Verify(t => t.AddAsync(
            It.Is<HatGapTransaction>(tx => tx.Source == "StreakMilestone7"), default),
            Times.Never);
    }

    [Fact]
    public async Task AwardStreakMilestone_StreakReaches30_Awards20HatGap()
    {
        // Arrange: CurrentStreak=29 + one more consecutive day => 30
        var userId = Guid.NewGuid();
        var command = ArrangeStreak(userId, Guid.NewGuid(), Guid.NewGuid(), currentStreakBeforeStep: 29);
        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        _hatGapTransactions.Verify(t => t.AddAsync(
            It.Is<HatGapTransaction>(tx =>
                tx.Amount == 20 &&
                tx.Type == HatGapTransactionType.Earn &&
                tx.Source == "StreakMilestone30"),
            default),
            Times.Once);
    }

    [Fact]
    public async Task AwardStreakMilestone_StreakReaches6_NoHatGapAwarded()
    {
        // Arrange: CurrentStreak=5 + one more consecutive day => 6 (not a milestone)
        var userId = Guid.NewGuid();
        var command = ArrangeStreak(userId, Guid.NewGuid(), Guid.NewGuid(), currentStreakBeforeStep: 5);
        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        _hatGapTransactions.Verify(t => t.AddAsync(It.IsAny<HatGapTransaction>(), default), Times.Never);
        _notifications.Verify(n => n.NotifyUserAsync(
            userId,
            NotificationType.StreakMilestoneReached,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            default),
            Times.Never);
    }
}
