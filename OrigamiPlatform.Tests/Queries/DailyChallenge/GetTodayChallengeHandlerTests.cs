using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.DTOs.DailyChallenge;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Application.Queries.DailyChallenge;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrigamiPlatform.Tests.Queries.DailyChallenge;

public class GetTodayChallengeHandlerTests
{
    private readonly Mock<IDailyChallengeRepository> _mockChallenges;
    private readonly Mock<IDailyChallengeSubmissionRepository> _mockSubmissions;
    private readonly Mock<IChallengeStreakRepository> _mockStreaks;
    private readonly GetTodayChallengeHandler _handler;

    public GetTodayChallengeHandlerTests()
    {
        _mockChallenges = new Mock<IDailyChallengeRepository>();
        _mockSubmissions = new Mock<IDailyChallengeSubmissionRepository>();
        _mockStreaks = new Mock<IChallengeStreakRepository>();
        _handler = new GetTodayChallengeHandler(_mockChallenges.Object, _mockSubmissions.Object, _mockStreaks.Object);
    }

    [Fact]
    public async Task HandleAsync_NoChallengeToday_ThrowsNotFoundException()
    {
        // Arrange
        var query = new GetTodayChallengeQuery(null);
        _mockChallenges.Setup(x => x.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrigamiPlatform.Domain.Entities.DailyChallenge?)null);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(query);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Hôm nay chưa có Thử thách ngày.");
    }

    [Fact]
    public async Task HandleAsync_AnonymousUser_ReturnsChallengeData_WithoutUserSpecifics()
    {
        // Arrange
        var query = new GetTodayChallengeQuery(null);
        var challenge = new OrigamiPlatform.Domain.Entities.DailyChallenge
        {
            Id = Guid.NewGuid(),
            ChallengeDate = DateOnly.FromDateTime(DateTime.Now),
            Status = DailyChallengeStatus.Active,
            TutorialId = Guid.NewGuid(),
            Tutorial = new Tutorial 
            { 
                Title = "Origami Crane",
                Slug = "origami-crane",
                Difficulty = TutorialDifficulty.Beginner,
                AuthorId = Guid.NewGuid(),
                Author = new User { Profile = new UserProfile { DisplayName = "Origami Master" } }
            }
        };
        var submissions = new List<DailyChallengeSubmission>
        {
            new DailyChallengeSubmission { Id = Guid.NewGuid() },
            new DailyChallengeSubmission { Id = Guid.NewGuid() }
        };

        _mockChallenges.Setup(x => x.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        _mockSubmissions.Setup(x => x.GetByChallengeAsync(challenge.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submissions);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(challenge.Id);
        result.HasSubmittedToday.Should().BeNull();
        result.MyChallengeStreak.Should().BeNull();
        result.SubmissionCount.Should().Be(2);
        result.TutorialTitle.Should().Be("Origami Crane");
    }

    [Fact]
    public async Task HandleAsync_AuthenticatedUser_ReturnsChallengeData_WithUserSpecifics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetTodayChallengeQuery(userId);
        var challenge = new OrigamiPlatform.Domain.Entities.DailyChallenge
        {
            Id = Guid.NewGuid(),
            ChallengeDate = DateOnly.FromDateTime(DateTime.Now),
            Status = DailyChallengeStatus.Active,
            TutorialId = Guid.NewGuid(),
            Tutorial = new Tutorial 
            { 
                Title = "Origami Frog",
                Slug = "origami-frog",
                Difficulty = TutorialDifficulty.Intermediate,
                AuthorId = Guid.NewGuid(),
                Author = new User { Profile = new UserProfile { DisplayName = "Origami Master" } }
            }
        };

        var submissions = new List<DailyChallengeSubmission>
        {
            new DailyChallengeSubmission { Id = Guid.NewGuid(), UserId = userId }
        };

        var streakLog = new ChallengeStreakLog { CurrentStreak = 5, UserId = userId };

        _mockChallenges.Setup(x => x.GetByDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        _mockSubmissions.Setup(x => x.GetByChallengeAsync(challenge.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submissions);

        _mockStreaks.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(streakLog);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.HasSubmittedToday.Should().BeTrue();
        result.MyChallengeStreak.Should().Be(5);
        result.SubmissionCount.Should().Be(1);
    }
}
