using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.WeeklyChallenge;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Tests.Commands.WeeklyChallenge;

public class CloseWeeklyChallengeResultHandlerTests
{
    private readonly Mock<IWeeklyChallengeRepository> _mockChallenges;
    private readonly Mock<IWeeklyChallengeSubmissionRepository> _mockSubmissions;
    private readonly Mock<ILikeRepository> _mockLikes;
    private readonly Mock<IHatGapTransactionRepository> _mockHatGapRepo;
    private readonly Mock<INotificationService> _mockNotifications;
    private readonly CloseWeeklyChallengeResultHandler _handler;

    public CloseWeeklyChallengeResultHandlerTests()
    {
        _mockChallenges = new Mock<IWeeklyChallengeRepository>();
        _mockSubmissions = new Mock<IWeeklyChallengeSubmissionRepository>();
        _mockLikes = new Mock<ILikeRepository>();
        _mockHatGapRepo = new Mock<IHatGapTransactionRepository>();
        _mockNotifications = new Mock<INotificationService>();

        var hatGapService = new HatGapAwardService(_mockHatGapRepo.Object);

        _handler = new CloseWeeklyChallengeResultHandler(
            _mockChallenges.Object,
            _mockSubmissions.Object,
            _mockLikes.Object,
            hatGapService,
            _mockNotifications.Object
        );
    }

    [Fact]
    public async Task HandleAsync_ChallengeNotFound_DoesNothing()
    {
        var command = new CloseWeeklyChallengeResultCommand(Guid.NewGuid());
        _mockChallenges.Setup(c => c.GetByIdAsync(command.ChallengeId, default)).ReturnsAsync((OrigamiPlatform.Domain.Entities.WeeklyChallenge?)null);

        await _handler.HandleAsync(command);

        _mockSubmissions.Verify(s => s.GetByChallengeAsync(It.IsAny<Guid>(), default), Times.Never);
        _mockChallenges.Verify(c => c.UpdateAsync(It.IsAny<OrigamiPlatform.Domain.Entities.WeeklyChallenge>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ChallengeAlreadyClosed_DoesNothing()
    {
        var command = new CloseWeeklyChallengeResultCommand(Guid.NewGuid());
        var challenge = new OrigamiPlatform.Domain.Entities.WeeklyChallenge { Id = command.ChallengeId, Status = WeeklyChallengeStatus.Closed };
        _mockChallenges.Setup(c => c.GetByIdAsync(command.ChallengeId, default)).ReturnsAsync(challenge);

        await _handler.HandleAsync(command);

        _mockSubmissions.Verify(s => s.GetByChallengeAsync(It.IsAny<Guid>(), default), Times.Never);
        _mockChallenges.Verify(c => c.UpdateAsync(It.IsAny<OrigamiPlatform.Domain.Entities.WeeklyChallenge>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NoSubmissions_ClosesChallenge()
    {
        var command = new CloseWeeklyChallengeResultCommand(Guid.NewGuid());
        var challenge = new OrigamiPlatform.Domain.Entities.WeeklyChallenge { Id = command.ChallengeId, Status = WeeklyChallengeStatus.Active };
        _mockChallenges.Setup(c => c.GetByIdAsync(command.ChallengeId, default)).ReturnsAsync(challenge);
        _mockSubmissions.Setup(s => s.GetByChallengeAsync(command.ChallengeId, default)).ReturnsAsync(new List<WeeklyChallengeSubmission>());

        await _handler.HandleAsync(command);

        Assert.Equal(WeeklyChallengeStatus.Closed, challenge.Status);
        _mockSubmissions.Verify(s => s.UpdateRangeAsync(It.IsAny<List<WeeklyChallengeSubmission>>(), default), Times.Never);
        _mockChallenges.Verify(c => c.UpdateAsync(challenge, default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithSubmissions_RanksAndRewardsTop10()
    {
        var command = new CloseWeeklyChallengeResultCommand(Guid.NewGuid());
        var challenge = new OrigamiPlatform.Domain.Entities.WeeklyChallenge { Id = command.ChallengeId, Status = WeeklyChallengeStatus.Active };
        _mockChallenges.Setup(c => c.GetByIdAsync(command.ChallengeId, default)).ReturnsAsync(challenge);

        var sub1 = new WeeklyChallengeSubmission { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow.AddMinutes(-5) }; // 2nd place (fewer likes)
        var sub2 = new WeeklyChallengeSubmission { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow.AddMinutes(-10) }; // 1st place (more likes)
        var sub3 = new WeeklyChallengeSubmission { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow.AddMinutes(-2) }; // 3rd place (tie likes with sub1 but newer)

        var submissions = new List<WeeklyChallengeSubmission> { sub1, sub2, sub3 };
        _mockSubmissions.Setup(s => s.GetByChallengeAsync(command.ChallengeId, default)).ReturnsAsync(submissions);

        var likeCounts = new Dictionary<Guid, int>
        {
            { sub1.Id, 5 },
            { sub2.Id, 10 },
            { sub3.Id, 5 } // Ties with sub1, but sub1 is older, so sub1 ranks higher
        };
        _mockLikes.Setup(l => l.GetCountsForTargetsAsync(It.IsAny<IEnumerable<Guid>>(), TargetType.WeeklyChallengeSubmission)).ReturnsAsync(likeCounts);

        await _handler.HandleAsync(command);

        Assert.Equal(WeeklyChallengeStatus.Closed, challenge.Status);
        
        // Verifying ranks
        Assert.Equal(2, sub1.FinalRank);
        Assert.Equal(1, sub2.FinalRank);
        Assert.Equal(3, sub3.FinalRank);

        _mockSubmissions.Verify(s => s.UpdateRangeAsync(It.Is<List<WeeklyChallengeSubmission>>(list => list.Count == 3), default), Times.Once);

        // Verifying rewards (Top 3 in this case)
        _mockHatGapRepo.Verify(h => h.AddAsync(It.Is<HatGapTransaction>(tx => tx.UserId == sub2.UserId && tx.Source == "WeeklyChallengeRank1"), default), Times.Once);
        _mockHatGapRepo.Verify(h => h.AddAsync(It.Is<HatGapTransaction>(tx => tx.UserId == sub1.UserId && tx.Source == "WeeklyChallengeRank2"), default), Times.Once);
        _mockHatGapRepo.Verify(h => h.AddAsync(It.Is<HatGapTransaction>(tx => tx.UserId == sub3.UserId && tx.Source == "WeeklyChallengeRank3"), default), Times.Once);
        
        _mockNotifications.Verify(n => n.NotifyUserAsync(sub2.UserId, NotificationType.WeeklyChallengeResult, It.IsAny<string>(), nameof(WeeklyChallengeSubmission), sub2.Id, default), Times.Once);
    }
}
