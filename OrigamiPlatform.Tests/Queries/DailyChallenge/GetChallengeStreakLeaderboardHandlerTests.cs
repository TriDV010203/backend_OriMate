using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.DTOs.DailyChallenge;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Application.Queries.DailyChallenge;
using OrigamiPlatform.Domain.Entities;
using Xunit;

namespace OrigamiPlatform.Tests.Queries.DailyChallenge;

public class GetChallengeStreakLeaderboardHandlerTests
{
    private readonly Mock<IChallengeStreakRepository> _mockStreaks;
    private readonly GetChallengeStreakLeaderboardHandler _handler;

    public GetChallengeStreakLeaderboardHandlerTests()
    {
        _mockStreaks = new Mock<IChallengeStreakRepository>();
        _handler = new GetChallengeStreakLeaderboardHandler(_mockStreaks.Object);
    }

    [Fact]
    public async Task HandleAsync_ReturnsLeaderboard()
    {
        // Arrange
        var query = new GetChallengeStreakLeaderboardQuery(10);
        var streaks = new List<ChallengeStreakLog>
        {
            new ChallengeStreakLog { UserId = Guid.NewGuid(), CurrentStreak = 5, LongestStreak = 10, User = new User { Profile = new UserProfile { DisplayName = "User 1" } } },
            new ChallengeStreakLog { UserId = Guid.NewGuid(), CurrentStreak = 3, LongestStreak = 8, User = new User { Profile = new UserProfile { DisplayName = "User 2" } } }
        };

        _mockStreaks.Setup(x => x.GetTopAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(streaks);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().HaveCount(2);
        result[0].Rank.Should().Be(1);
        result[0].DisplayName.Should().Be("User 1");
        result[0].CurrentStreak.Should().Be(5);
        result[1].Rank.Should().Be(2);
        result[1].DisplayName.Should().Be("User 2");
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(150, 100)]
    public async Task HandleAsync_ClampsTopValue(int inputTop, int expectedTop)
    {
        // Arrange
        var query = new GetChallengeStreakLeaderboardQuery(inputTop);
        _mockStreaks.Setup(x => x.GetTopAsync(expectedTop, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChallengeStreakLog>());

        // Act
        await _handler.HandleAsync(query);

        // Assert
        _mockStreaks.Verify(x => x.GetTopAsync(expectedTop, It.IsAny<CancellationToken>()), Times.Once);
    }
}
