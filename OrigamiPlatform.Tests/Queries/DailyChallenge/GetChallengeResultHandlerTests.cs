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

public class GetChallengeResultHandlerTests
{
    private readonly Mock<IDailyChallengeRepository> _mockChallenges;
    private readonly Mock<IDailyChallengeSubmissionRepository> _mockSubmissions;
    private readonly Mock<ILikeRepository> _mockLikes;
    private readonly GetChallengeResultHandler _handler;

    public GetChallengeResultHandlerTests()
    {
        _mockChallenges = new Mock<IDailyChallengeRepository>();
        _mockSubmissions = new Mock<IDailyChallengeSubmissionRepository>();
        _mockLikes = new Mock<ILikeRepository>();
        _handler = new GetChallengeResultHandler(_mockChallenges.Object, _mockSubmissions.Object, _mockLikes.Object);
    }

    [Fact]
    public async Task HandleAsync_ChallengeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var query = new GetChallengeResultQuery(DateOnly.FromDateTime(DateTime.Now));

        _mockChallenges.Setup(x => x.GetByDateAsync(query.ChallengeDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrigamiPlatform.Domain.Entities.DailyChallenge?)null);

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(query);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Không tìm thấy Thử thách ngày cho ngày này.");
    }

    [Fact]
    public async Task HandleAsync_ReturnsTopSubmissions()
    {
        // Arrange
        var query = new GetChallengeResultQuery(DateOnly.FromDateTime(DateTime.Now));
        var challengeId = Guid.NewGuid();
        var tutorialId = Guid.NewGuid();

        var challenge = new OrigamiPlatform.Domain.Entities.DailyChallenge
        {
            Id = challengeId,
            ChallengeDate = query.ChallengeDate,
            Status = DailyChallengeStatus.Active,
            TutorialId = tutorialId,
            Tutorial = new Tutorial { Title = "Origami Crane" }
        };

        var user1 = new User { Id = Guid.NewGuid(), Profile = new UserProfile { DisplayName = "User 1", AvatarUrl = "url1" } };
        var user2 = new User { Id = Guid.NewGuid(), Profile = new UserProfile { DisplayName = "User 2", AvatarUrl = "url2" } };

        var submissions = new List<DailyChallengeSubmission>
        {
            new DailyChallengeSubmission { Id = Guid.NewGuid(), DailyChallengeId = challengeId, FinalRank = 1, User = user1, CreatedAt = DateTime.UtcNow },
            new DailyChallengeSubmission { Id = Guid.NewGuid(), DailyChallengeId = challengeId, FinalRank = 2, User = user2, CreatedAt = DateTime.UtcNow },
            new DailyChallengeSubmission { Id = Guid.NewGuid(), DailyChallengeId = challengeId, FinalRank = 4, User = new User(), CreatedAt = DateTime.UtcNow }
        };

        var likeCounts = new Dictionary<Guid, int>
        {
            { submissions.ElementAt(0).Id, 10 },
            { submissions[1].Id, 5 }
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
        result.ChallengeDate.Should().Be(query.ChallengeDate);
        result.Status.Should().Be(DailyChallengeStatus.Active.ToString());
        result.TutorialId.Should().Be(tutorialId);
        result.TutorialTitle.Should().Be("Origami Crane");
        result.TotalParticipants.Should().Be(3);
        result.TopSubmissions.Should().HaveCount(2); // Only rank 1 and 2 (<= 3)
        result.TopSubmissions.First().LikeCount.Should().Be(10);
    }
}
