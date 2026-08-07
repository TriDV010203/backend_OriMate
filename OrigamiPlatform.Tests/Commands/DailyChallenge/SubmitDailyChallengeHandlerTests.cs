using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.DailyChallenge;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.DailyChallenge;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.DailyChallenge;

public class SubmitDailyChallengeHandlerTests
{
    private readonly Mock<IDailyChallengeRepository> _mockChallenges;
    private readonly Mock<IDailyChallengeSubmissionRepository> _mockSubmissions;
    private readonly Mock<IChallengeStreakRepository> _mockChallengeStreaks;
    private readonly Mock<IBlockedWordService> _mockBlockedWordService;
    private readonly Mock<IHatGapTransactionRepository> _mockHatGapRepo;
    private readonly Mock<IBadgeRepository> _mockBadgeRepo;
    private readonly Mock<IUserBadgeRepository> _mockUserBadgeRepo;
    private readonly Mock<INotificationService> _mockNotifications;
    private readonly SubmitDailyChallengeHandler _handler;

    public SubmitDailyChallengeHandlerTests()
    {
        _mockChallenges = new Mock<IDailyChallengeRepository>();
        _mockSubmissions = new Mock<IDailyChallengeSubmissionRepository>();
        _mockChallengeStreaks = new Mock<IChallengeStreakRepository>();
        _mockBlockedWordService = new Mock<IBlockedWordService>();
        
        _mockHatGapRepo = new Mock<IHatGapTransactionRepository>();
        _mockBadgeRepo = new Mock<IBadgeRepository>();
        _mockUserBadgeRepo = new Mock<IUserBadgeRepository>();
        _mockNotifications = new Mock<INotificationService>();

        var hatGapService = new HatGapAwardService(_mockHatGapRepo.Object);
        var badgeService = new BadgeAwardService(_mockBadgeRepo.Object, _mockUserBadgeRepo.Object, _mockNotifications.Object);

        _handler = new SubmitDailyChallengeHandler(
            _mockChallenges.Object, _mockSubmissions.Object, _mockChallengeStreaks.Object, 
            _mockBlockedWordService.Object, hatGapService, badgeService);
    }

    [Fact]
    public async Task HandleAsync_EmptyPhotoUrl_ThrowsDomainException()
    {
        var req = new SubmitDailyChallengeRequest(string.Empty, "");
        var command = new SubmitDailyChallengeCommand(Guid.NewGuid(), req);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("bắt buộc", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ContainsBlockedWord_ThrowsDomainException()
    {
        var req = new SubmitDailyChallengeRequest("photo.jpg", "badword");
        var command = new SubmitDailyChallengeCommand(Guid.NewGuid(), req);

        _mockBlockedWordService.Setup(b => b.ContainsBlockedWordAsync(req.Note, default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("từ ngữ không phù hợp", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_NoChallengeToday_ThrowsNotFoundException()
    {
        var req = new SubmitDailyChallengeRequest("photo.jpg", "Good");
        var command = new SubmitDailyChallengeCommand(Guid.NewGuid(), req);

        _mockBlockedWordService.Setup(b => b.ContainsBlockedWordAsync(req.Note, default)).ReturnsAsync(false);
        _mockChallenges.Setup(c => c.GetByDateAsync(It.IsAny<DateOnly>(), default)).ReturnsAsync((OrigamiPlatform.Domain.Entities.DailyChallenge?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Contains("chưa có Thử thách", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ChallengeNotActive_ThrowsDomainException()
    {
        var req = new SubmitDailyChallengeRequest("photo.jpg", "Good");
        var command = new SubmitDailyChallengeCommand(Guid.NewGuid(), req);
        var challenge = new OrigamiPlatform.Domain.Entities.DailyChallenge { Status = DailyChallengeStatus.Closed };

        _mockBlockedWordService.Setup(b => b.ContainsBlockedWordAsync(req.Note, default)).ReturnsAsync(false);
        _mockChallenges.Setup(c => c.GetByDateAsync(It.IsAny<DateOnly>(), default)).ReturnsAsync(challenge);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("chưa mở hoặc đã đóng", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_AlreadySubmitted_ThrowsDomainException()
    {
        var req = new SubmitDailyChallengeRequest("photo.jpg", "Good");
        var command = new SubmitDailyChallengeCommand(Guid.NewGuid(), req);
        var challenge = new OrigamiPlatform.Domain.Entities.DailyChallenge { Id = Guid.NewGuid(), Status = DailyChallengeStatus.Active };

        _mockBlockedWordService.Setup(b => b.ContainsBlockedWordAsync(req.Note, default)).ReturnsAsync(false);
        _mockChallenges.Setup(c => c.GetByDateAsync(It.IsAny<DateOnly>(), default)).ReturnsAsync(challenge);
        _mockSubmissions.Setup(s => s.ExistsAsync(challenge.Id, command.UserId, default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("đã nộp bài", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_AddsSubmissionAndUpdatesStreak()
    {
        var req = new SubmitDailyChallengeRequest("photo.jpg", "Good");
        var command = new SubmitDailyChallengeCommand(Guid.NewGuid(), req);
        var challenge = new OrigamiPlatform.Domain.Entities.DailyChallenge { Id = Guid.NewGuid(), Status = DailyChallengeStatus.Active };
        var streak = new ChallengeStreakLog { UserId = command.UserId, CurrentStreak = 1, LastSubmissionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)) };

        _mockBlockedWordService.Setup(b => b.ContainsBlockedWordAsync(req.Note, default)).ReturnsAsync(false);
        _mockChallenges.Setup(c => c.GetByDateAsync(It.IsAny<DateOnly>(), default)).ReturnsAsync(challenge);
        _mockSubmissions.Setup(s => s.ExistsAsync(challenge.Id, command.UserId, default)).ReturnsAsync(false);
        _mockChallengeStreaks.Setup(s => s.GetByUserIdAsync(command.UserId, default)).ReturnsAsync(streak);

        var result = await _handler.HandleAsync(command);

        Assert.NotNull(result);
        Assert.Equal(req.PhotoUrl, result.PhotoUrl);

        _mockSubmissions.Verify(s => s.AddAsync(It.Is<DailyChallengeSubmission>(sub => 
            sub.UserId == command.UserId && 
            sub.DailyChallengeId == challenge.Id), default), Times.Once);

        _mockChallengeStreaks.Verify(s => s.UpdateAsync(It.Is<ChallengeStreakLog>(sl => sl.CurrentStreak == 2), default), Times.Once);
        _mockHatGapRepo.Verify(r => r.AddAsync(It.IsAny<HatGapTransaction>(), default), Times.Once);
    }
}
