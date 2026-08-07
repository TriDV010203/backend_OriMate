using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.DTOs.DailyChallenge;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Application.Queries.DailyChallenge;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrigamiPlatform.Tests.Queries.DailyChallenge;

public class GetChallengeSuggestionsHandlerTests
{
    private readonly Mock<IDailyChallengeRepository> _mockChallenges;
    private readonly GetChallengeSuggestionsHandler _handler;

    public GetChallengeSuggestionsHandlerTests()
    {
        _mockChallenges = new Mock<IDailyChallengeRepository>();
        _handler = new GetChallengeSuggestionsHandler(_mockChallenges.Object);
    }

    [Fact]
    public async Task HandleAsync_ReturnsSuggestions_OrderedByAchievementCount()
    {
        // Arrange
        var query = new GetChallengeSuggestionsQuery(10);
        var excludeIds = new HashSet<Guid> { Guid.NewGuid() };

        _mockChallenges.Setup(x => x.GetRecentlyUsedTutorialIdsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(excludeIds);

        var candidates = new List<(Tutorial Tutorial, int AchievementCount)>
        {
            (new Tutorial { Id = Guid.NewGuid(), Title = "A", Slug = "a", CoverImageUrl = "a.jpg", Difficulty = TutorialDifficulty.Beginner, CategoryId = 1 }, 5),
            (new Tutorial { Id = Guid.NewGuid(), Title = "B", Slug = "b", CoverImageUrl = "b.jpg", Difficulty = TutorialDifficulty.Intermediate, CategoryId = 2 }, 15)
        };

        _mockChallenges.Setup(x => x.GetEligibleCandidatesAsync(excludeIds, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().HaveCount(2);
        result.ElementAt(0).Title.Should().Be("B"); // Higher achievement count first
        result.ElementAt(1).Title.Should().Be("A");
    }

    [Fact]
    public async Task HandleAsync_WhenEligibleCandidatesEmpty_FallsBackToAllCandidates()
    {
        // Arrange
        var query = new GetChallengeSuggestionsQuery(10);
        var excludeIds = new HashSet<Guid> { Guid.NewGuid() };

        _mockChallenges.Setup(x => x.GetRecentlyUsedTutorialIdsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(excludeIds);

        _mockChallenges.Setup(x => x.GetEligibleCandidatesAsync(excludeIds, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Tutorial Tutorial, int AchievementCount)>()); // Empty initial candidates

        var fallbackCandidates = new List<(Tutorial Tutorial, int AchievementCount)>
        {
            (new Tutorial { Id = Guid.NewGuid(), Title = "Fallback", Slug = "f", CoverImageUrl = "f.jpg", Difficulty = TutorialDifficulty.Beginner, CategoryId = 1 }, 1)
        };

        _mockChallenges.Setup(x => x.GetEligibleCandidatesAsync(It.Is<HashSet<Guid>>(h => h.Count == 0), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fallbackCandidates);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().HaveCount(1);
        result.ElementAt(0).Title.Should().Be("Fallback");
    }
}
