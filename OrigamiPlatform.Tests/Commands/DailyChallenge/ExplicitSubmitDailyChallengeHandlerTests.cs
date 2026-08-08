using Moq;
using OrigamiPlatform.Application.Commands.DailyChallenge;
using OrigamiPlatform.Application.Common;
using OrigamiPlatform.Application.DTOs.DailyChallenge;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrigamiPlatform.Tests.Commands.DailyChallenge
{
    public class ExplicitSubmitDailyChallengeHandlerTests
    {
        private readonly Mock<IDailyChallengeRepository> _challengesMock;
        private readonly Mock<IDailyChallengeSubmissionRepository> _submissionsMock;
        private readonly Mock<IChallengeStreakRepository> _challengeStreaksMock;
        private readonly Mock<IBlockedWordService> _blockedWordServiceMock;
        private readonly Mock<IHatGapTransactionRepository> _hatGapTransactionRepoMock;
        private readonly Mock<IBadgeRepository> _badgeRepoMock;
        private readonly Mock<IUserBadgeRepository> _userBadgeRepoMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly SubmitDailyChallengeHandler _handler;

        public ExplicitSubmitDailyChallengeHandlerTests()
        {
            _challengesMock = new Mock<IDailyChallengeRepository>();
            _submissionsMock = new Mock<IDailyChallengeSubmissionRepository>();
            _challengeStreaksMock = new Mock<IChallengeStreakRepository>();
            _blockedWordServiceMock = new Mock<IBlockedWordService>();
            
            _hatGapTransactionRepoMock = new Mock<IHatGapTransactionRepository>();
            var hatGapAwardService = new HatGapAwardService(_hatGapTransactionRepoMock.Object);

            _badgeRepoMock = new Mock<IBadgeRepository>();
            _userBadgeRepoMock = new Mock<IUserBadgeRepository>();
            _notificationServiceMock = new Mock<INotificationService>();
            var badgeAwardService = new BadgeAwardService(_badgeRepoMock.Object, _userBadgeRepoMock.Object, _notificationServiceMock.Object);

            _handler = new SubmitDailyChallengeHandler(
                _challengesMock.Object,
                _submissionsMock.Object,
                _challengeStreaksMock.Object,
                _blockedWordServiceMock.Object,
                hatGapAwardService,
                badgeAwardService);
        }

        [Fact]
        public async Task HandleAsync_ValidRequest_Success()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var challengeId = Guid.NewGuid();
            var command = new SubmitDailyChallengeCommand(userId, new SubmitDailyChallengeRequest("https://example.com/photo.jpg", "Great challenge"));
            
            _blockedWordServiceMock.Setup(x => x.ContainsBlockedWordAsync("Great challenge", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
                
            var challenge = new OrigamiPlatform.Domain.Entities.DailyChallenge
            {
                Id = challengeId,
                Status = DailyChallengeStatus.Active
            };
            
            _challengesMock.Setup(x => x.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(challenge);

            _submissionsMock.Setup(x => x.ExistsAsync(challengeId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var streakLog = new ChallengeStreakLog
            {
                UserId = userId,
                CurrentStreak = 0,
                LongestStreak = 0,
                LastSubmissionDate = null,
                FreezeCount = 0
            };
            _challengeStreaksMock.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(streakLog);

            _hatGapTransactionRepoMock.Setup(x => x.GetLatestBalanceAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(100);

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
            Assert.Equal("https://example.com/photo.jpg", result.PhotoUrl);
            Assert.Equal("Great challenge", result.Note);
            
            _submissionsMock.Verify(x => x.AddAsync(It.IsAny<DailyChallengeSubmission>(), It.IsAny<CancellationToken>()), Times.Once);
            _challengeStreaksMock.Verify(x => x.UpdateAsync(It.IsAny<ChallengeStreakLog>(), It.IsAny<CancellationToken>()), Times.Once);
            _hatGapTransactionRepoMock.Verify(x => x.AddAsync(It.IsAny<HatGapTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
