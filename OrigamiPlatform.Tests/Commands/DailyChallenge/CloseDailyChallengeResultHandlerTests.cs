using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.DailyChallenge;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Tests.Commands.DailyChallenge;

public class CloseDailyChallengeResultHandlerTests
{
    private readonly Mock<IDailyChallengeRepository> _mockChallenges;
    private readonly Mock<IDailyChallengeSubmissionRepository> _mockSubmissions;
    private readonly Mock<ILikeRepository> _mockLikes;
    private readonly Mock<IChallengeStreakRepository> _mockChallengeStreaks;
    private readonly Mock<IHatGapTransactionRepository> _mockHatGapRepo;
    private readonly Mock<IBadgeRepository> _mockBadgeRepo;
    private readonly Mock<IUserBadgeRepository> _mockUserBadgeRepo;
    private readonly Mock<INotificationService> _mockNotifications;
    private readonly CloseDailyChallengeResultHandler _handler;

    public CloseDailyChallengeResultHandlerTests()
    {
        _mockChallenges = new Mock<IDailyChallengeRepository>();
        _mockSubmissions = new Mock<IDailyChallengeSubmissionRepository>();
        _mockLikes = new Mock<ILikeRepository>();
        _mockChallengeStreaks = new Mock<IChallengeStreakRepository>();
        _mockHatGapRepo = new Mock<IHatGapTransactionRepository>();
        _mockBadgeRepo = new Mock<IBadgeRepository>();
        _mockUserBadgeRepo = new Mock<IUserBadgeRepository>();
        _mockNotifications = new Mock<INotificationService>();

        var hatGapService = new HatGapAwardService(_mockHatGapRepo.Object);
        var badgeService = new BadgeAwardService(_mockBadgeRepo.Object, _mockUserBadgeRepo.Object, _mockNotifications.Object);

        _handler = new CloseDailyChallengeResultHandler(
            _mockChallenges.Object, _mockSubmissions.Object, _mockLikes.Object, _mockChallengeStreaks.Object, 
            hatGapService, badgeService, _mockNotifications.Object);
    }

    [Fact]
    public async Task HandleAsync_NoChallengeOrNotActive_NoOps()
    {
        var command = new CloseDailyChallengeResultCommand(DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)));
        _mockChallenges.Setup(c => c.GetByDateAsync(command.ChallengeDate, default)).ReturnsAsync((OrigamiPlatform.Domain.Entities.DailyChallenge?)null);

        await _handler.HandleAsync(command);

        _mockSubmissions.Verify(s => s.GetByChallengeAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NoSubmissions_UpdatesStatusOnly()
    {
        var command = new CloseDailyChallengeResultCommand(DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)));
        var challenge = new OrigamiPlatform.Domain.Entities.DailyChallenge { Id = Guid.NewGuid(), Status = DailyChallengeStatus.Active };

        _mockChallenges.Setup(c => c.GetByDateAsync(command.ChallengeDate, default)).ReturnsAsync(challenge);
        _mockSubmissions.Setup(s => s.GetByChallengeAsync(challenge.Id, default)).ReturnsAsync(new List<DailyChallengeSubmission>());

        await _handler.HandleAsync(command);

        Assert.Equal(DailyChallengeStatus.Closed, challenge.Status);
        _mockChallenges.Verify(c => c.UpdateAsync(challenge, default), Times.Once);
        _mockSubmissions.Verify(s => s.UpdateRangeAsync(It.IsAny<List<DailyChallengeSubmission>>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithSubmissions_CalculatesRanksAndRewardsTop3()
    {
        var command = new CloseDailyChallengeResultCommand(DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)));
        var challenge = new OrigamiPlatform.Domain.Entities.DailyChallenge { Id = Guid.NewGuid(), Status = DailyChallengeStatus.Active };
        
        var sub1 = new DailyChallengeSubmission { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };
        var sub2 = new DailyChallengeSubmission { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };
        
        _mockChallenges.Setup(c => c.GetByDateAsync(command.ChallengeDate, default)).ReturnsAsync(challenge);
        _mockSubmissions.Setup(s => s.GetByChallengeAsync(challenge.Id, default)).ReturnsAsync(new List<DailyChallengeSubmission> { sub1, sub2 });
        _mockLikes.Setup(l => l.GetCountsForTargetsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<TargetType>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { sub1.Id, 5 }, { sub2.Id, 10 } }); // sub2 has more likes -> rank 1

        _mockChallengeStreaks.Setup(s => s.GetByUserIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(new ChallengeStreakLog { UserId = sub2.UserId, FreezeCount = 0 });

        await _handler.HandleAsync(command);

        Assert.Equal(DailyChallengeStatus.Closed, challenge.Status);
        Assert.Equal(1, sub2.FinalRank);
        Assert.Equal(2, sub1.FinalRank);

        _mockSubmissions.Verify(s => s.UpdateRangeAsync(It.IsAny<List<DailyChallengeSubmission>>(), default), Times.Once);
        _mockHatGapRepo.Verify(r => r.AddAsync(It.IsAny<HatGapTransaction>(), default), Times.Exactly(2)); // Reward for Rank 1 and 2
    }
}
