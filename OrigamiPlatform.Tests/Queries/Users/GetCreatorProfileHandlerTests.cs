using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.Queries.Users;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;
using Xunit;

namespace OrigamiPlatform.Tests.Queries.Users;

public class GetCreatorProfileHandlerTests
{
    private readonly Mock<IUserRepository> _usersMock;
    private readonly Mock<IFollowRepository> _followsMock;
    private readonly Mock<ICommunityPostRepository> _postsMock;
    private readonly Mock<IAchievementRepository> _achievementsMock;
    private readonly GetCreatorProfileHandler _handler;

    public GetCreatorProfileHandlerTests()
    {
        _usersMock = new Mock<IUserRepository>();
        _followsMock = new Mock<IFollowRepository>();
        _postsMock = new Mock<ICommunityPostRepository>();
        _achievementsMock = new Mock<IAchievementRepository>();

        _handler = new GetCreatorProfileHandler(
            _usersMock.Object,
            _followsMock.Object,
            _postsMock.Object,
            _achievementsMock.Object);
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var query = new GetCreatorProfileQuery(Guid.NewGuid(), null);
        _usersMock.Setup(x => x.GetByIdAsync(query.TargetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await FluentActions.Invoking(() => _handler.HandleAsync(query))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("Creator profile not found.");
    }

    [Fact]
    public async Task HandleAsync_ValidQuery_ReturnsCreatorProfileDto()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var query = new GetCreatorProfileQuery(targetUserId, currentUserId);

        var user = new User
        {
            Id = targetUserId,
            Status = AccountStatus.Active,
            Profile = new UserProfile
            {
                DisplayName = "Test Creator",
                AvatarUrl = "http://example.com/avatar.jpg",
                Bio = "Creator Bio"
            },
            Roles = new List<UserRole> { new UserRole { Role = UserRoleType.ContributorReviewer } }
        };

        _usersMock.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _followsMock.Setup(x => x.GetFollowersCountAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        _followsMock.Setup(x => x.GetFollowingCountAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        _postsMock.Setup(x => x.GetPostCountByAuthorAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(20);

        var achievementItems = new List<Achievement>();
        _achievementsMock.Setup(x => x.GetByUserAsync(targetUserId, false, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((achievementItems, 5));

        _followsMock.Setup(x => x.GetFollowAsync(currentUserId, targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FollowRelationship { FollowerId = currentUserId, FollowingId = targetUserId });

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(targetUserId);
        result.DisplayName.Should().Be("Test Creator");
        result.AvatarUrl.Should().Be("http://example.com/avatar.jpg");
        result.Bio.Should().Be("Creator Bio");
        result.FollowerCount.Should().Be(100);
        result.FollowingCount.Should().Be(50);
        result.PostCount.Should().Be(20);
        result.AchievementCount.Should().Be(5);
        result.IsFollowing.Should().BeTrue();
        result.IsSuspended.Should().BeFalse();
        result.Roles.Should().Contain("ContributorReviewer");
    }

    [Fact]
    public async Task HandleAsync_NotFollowing_ReturnsIsFollowingFalse()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var query = new GetCreatorProfileQuery(targetUserId, currentUserId);

        var user = new User
        {
            Id = targetUserId,
            Status = AccountStatus.Active,
            Profile = new UserProfile(),
            Roles = new List<UserRole>()
        };

        _usersMock.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var achievementItems = new List<Achievement>();
        _achievementsMock.Setup(x => x.GetByUserAsync(targetUserId, false, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((achievementItems, 0));

        _followsMock.Setup(x => x.GetFollowAsync(currentUserId, targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FollowRelationship?)null);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.IsFollowing.Should().BeFalse();
    }
}
