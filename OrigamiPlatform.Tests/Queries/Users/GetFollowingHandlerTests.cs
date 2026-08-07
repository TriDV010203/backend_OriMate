using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Queries.Users;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.Tests.Queries.Users;

public class GetFollowingHandlerTests
{
    private readonly Mock<IFollowRepository> _followsMock;
    private readonly Mock<ITutorialRepository> _tutorialsMock;
    private readonly GetFollowingHandler _handler;

    public GetFollowingHandlerTests()
    {
        _followsMock = new Mock<IFollowRepository>();
        _tutorialsMock = new Mock<ITutorialRepository>();
        _handler = new GetFollowingHandler(_followsMock.Object, _tutorialsMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidQuery_ReturnsPagedResult()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var followingUserId = Guid.NewGuid();
        var query = new GetFollowingQuery(targetUserId, currentUserId, 1, 10);

        var followingUser = new User
        {
            Id = followingUserId,
            Profile = new UserProfile
            {
                DisplayName = "Following",
                AvatarUrl = "url",
                Bio = "Bio"
            },
            Roles = new List<UserRole> { new UserRole { Role = UserRoleType.User } }
        };

        var pagedUsers = new PagedResult<User>(new List<User> { followingUser }, 1, 1, 10, 1);
        _followsMock.Setup(x => x.GetFollowingAsync(targetUserId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedUsers);

        _followsMock.Setup(x => x.GetFollowersCountAsync(followingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(20);

        var pagedTutorials = new PagedResult<Tutorial>(new List<Tutorial>(), 5, 1, 1, 5);
        _tutorialsMock.Setup(x => x.GetByAuthorAsync(followingUserId, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedTutorials);

        _followsMock.Setup(x => x.GetFollowAsync(currentUserId, followingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FollowRelationship?)null);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        
        var dto = result.Items.First();
        dto.UserId.Should().Be(followingUserId);
        dto.DisplayName.Should().Be("Following");
        dto.FollowerCount.Should().Be(20);
        dto.TutorialCount.Should().Be(5);
        dto.IsFollowing.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_EmptyFollowing_ReturnsEmptyPagedResult()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var query = new GetFollowingQuery(targetUserId, null, 1, 10);

        var pagedUsers = new PagedResult<User>(new List<User>(), 0, 1, 10, 0);
        _followsMock.Setup(x => x.GetFollowingAsync(targetUserId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedUsers);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}
