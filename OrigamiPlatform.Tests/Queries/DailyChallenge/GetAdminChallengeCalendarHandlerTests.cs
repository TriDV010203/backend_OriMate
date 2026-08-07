using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.DTOs.DailyChallenge;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Application.Queries.DailyChallenge;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.Tests.Queries.DailyChallenge;

public class GetAdminChallengeCalendarHandlerTests
{
    private readonly Mock<IDailyChallengeRepository> _mockChallenges;
    private readonly GetAdminChallengeCalendarHandler _handler;

    public GetAdminChallengeCalendarHandlerTests()
    {
        _mockChallenges = new Mock<IDailyChallengeRepository>();
        _handler = new GetAdminChallengeCalendarHandler(_mockChallenges.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidQuery_ReturnsPagedResult()
    {
        // Arrange
        var query = new GetAdminChallengeCalendarQuery(
            DateOnly.FromDateTime(DateTime.Now.AddDays(-7)),
            DateOnly.FromDateTime(DateTime.Now),
            DailyChallengeStatus.Active,
            1, 
            10);

        var challenges = new List<OrigamiPlatform.Domain.Entities.DailyChallenge>
        {
            new OrigamiPlatform.Domain.Entities.DailyChallenge
            {
                Id = Guid.NewGuid(),
                ChallengeDate = DateOnly.FromDateTime(DateTime.Now),
                Status = DailyChallengeStatus.Active,
                TutorialId = Guid.NewGuid(),
                Tutorial = new Tutorial { Title = "Test" }
            }
        };

        var pagedResult = new PagedResult<OrigamiPlatform.Domain.Entities.DailyChallenge>(challenges, 1, 1, 10, 1);

        _mockChallenges.Setup(x => x.GetAllForAdminAsync(
            query.FromDate, query.ToDate, query.Status, query.Page, query.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(0, 150, 1, 100)] // Page < 1 => 1, PageSize > 100 => 100
    [InlineData(-5, 0, 1, 1)]    // Page < 1 => 1, PageSize < 1 => 1
    public async Task HandleAsync_EnforcesPageAndPageSizeLimits(int inputPage, int inputPageSize, int expectedPage, int expectedPageSize)
    {
        // Arrange
        var query = new GetAdminChallengeCalendarQuery(null, null, null, inputPage, inputPageSize);

        var pagedResult = new PagedResult<OrigamiPlatform.Domain.Entities.DailyChallenge>(
            new List<OrigamiPlatform.Domain.Entities.DailyChallenge>(), 0, expectedPage, expectedPageSize, 0);

        _mockChallenges.Setup(x => x.GetAllForAdminAsync(
            It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<DailyChallengeStatus?>(), expectedPage, expectedPageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        _mockChallenges.Verify(x => x.GetAllForAdminAsync(
            It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<DailyChallengeStatus?>(), expectedPage, expectedPageSize, It.IsAny<CancellationToken>()), Times.Once);
        result.Page.Should().Be(expectedPage);
        result.PageSize.Should().Be(expectedPageSize);
    }
}
