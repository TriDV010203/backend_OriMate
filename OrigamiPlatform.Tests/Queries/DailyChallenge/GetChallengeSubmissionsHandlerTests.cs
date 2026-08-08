using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.DTOs.Common;
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

public class GetChallengeSubmissionsHandlerTests
{
    private readonly Mock<IDailyChallengeRepository> _mockChallenges;
    private readonly Mock<IDailyChallengeSubmissionRepository> _mockSubmissions;
    private readonly Mock<ILikeRepository> _mockLikes;
    private readonly GetChallengeSubmissionsHandler _handler;

    public GetChallengeSubmissionsHandlerTests()
    {
        _mockChallenges = new Mock<IDailyChallengeRepository>();
        _mockSubmissions = new Mock<IDailyChallengeSubmissionRepository>();
        _mockLikes = new Mock<ILikeRepository>();
        _handler = new GetChallengeSubmissionsHandler(_mockChallenges.Object, _mockSubmissions.Object, _mockLikes.Object);
    }

    [Fact]
    public async Task HandleAsync_ChallengeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var query = new GetChallengeSubmissionsQuery(DateOnly.FromDateTime(DateTime.Now), 1, 10, null);

        _mockChallenges.Setup(x => x.GetByDateAsync(query.ChallengeDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrigamiPlatform.Domain.Entities.DailyChallenge?)null);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(query);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Không tìm thấy Thử thách ngày cho ngày này.");
    }

    [Fact]
    public async Task HandleAsync_ReturnsPagedSubmissions_OrderedByLikesAndDate()
    {
        // Arrange
        var query = new GetChallengeSubmissionsQuery(DateOnly.FromDateTime(DateTime.Now), 1, 10, null);
        var challengeId = Guid.NewGuid();
        var challenge = new OrigamiPlatform.Domain.Entities.DailyChallenge { Id = challengeId, ChallengeDate = query.ChallengeDate };

        var user = new User { Profile = new UserProfile { DisplayName = "User" } };

        var submissions = new List<DailyChallengeSubmission>
        {
            new DailyChallengeSubmission { Id = Guid.NewGuid(), DailyChallengeId = challengeId, User = user, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
            new DailyChallengeSubmission { Id = Guid.NewGuid(), DailyChallengeId = challengeId, User = user, CreatedAt = DateTime.UtcNow.AddMinutes(-10) }
        };

        var likeCounts = new Dictionary<Guid, int>
        {
            { submissions.ElementAt(0).Id, 5 },
            { submissions[1].Id, 10 } // More likes, should be first
        };

        _mockChallenges.Setup(x => x.GetByDateAsync(query.ChallengeDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        _mockSubmissions.Setup(x => x.GetByChallengeAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submissions);

        _mockLikes.Setup(x => x.GetCountsForTargetsAsync(It.IsAny<IEnumerable<Guid>>(), TargetType.DailyChallengeSubmission))
            .ReturnsAsync(likeCounts);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.ElementAt(0).Id.Should().Be(submissions[1].Id); // Has more likes
        result.Items.ElementAt(1).Id.Should().Be(submissions.ElementAt(0).Id);
    }

    [Fact]
    public async Task HandleAsync_WithCurrentUser_ChecksLikedByMe()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var query = new GetChallengeSubmissionsQuery(DateOnly.FromDateTime(DateTime.Now), 1, 10, currentUserId);
        var challengeId = Guid.NewGuid();
        var challenge = new OrigamiPlatform.Domain.Entities.DailyChallenge { Id = challengeId, ChallengeDate = query.ChallengeDate };

        var submission = new DailyChallengeSubmission { Id = Guid.NewGuid(), DailyChallengeId = challengeId, User = new User { Profile = new UserProfile() }, CreatedAt = DateTime.UtcNow };

        _mockChallenges.Setup(x => x.GetByDateAsync(query.ChallengeDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        _mockSubmissions.Setup(x => x.GetByChallengeAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyChallengeSubmission> { submission });

        _mockLikes.Setup(x => x.GetCountsForTargetsAsync(It.IsAny<IEnumerable<Guid>>(), TargetType.DailyChallengeSubmission))
            .ReturnsAsync(new Dictionary<Guid, int>());

        _mockLikes.Setup(x => x.GetLikedTargetIdsAsync(currentUserId, It.IsAny<IEnumerable<Guid>>(), TargetType.DailyChallengeSubmission))
            .ReturnsAsync(new HashSet<Guid> { submission.Id });

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.ElementAt(0).IsLikedByCurrentUser.Should().BeTrue();
    }
}
