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

public class GetFollowersHandlerTests
{
    private readonly Mock<IFollowRepository> _followsMock;
    private readonly Mock<ITutorialRepository> _tutorialsMock;
    private readonly GetFollowersHandler _handler;

    public GetFollowersHandlerTests()
    {
        _followsMock = new Mock<IFollowRepository>();
        _tutorialsMock = new Mock<ITutorialRepository>();
        _handler = new GetFollowersHandler(_followsMock.Object, _tutorialsMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidQuery_ReturnsPagedResult()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var followerUserId = Guid.NewGuid();
        var query = new GetFollowersQuery(targetUserId, currentUserId, 1, 10);

        var followerUser = new User
        {
            Id = followerUserId,
            Profile = new UserProfile
            {
                DisplayName = "Follower",
                AvatarUrl = "url",
                Bio = "Bio"
            },
            Roles = new List<UserRole> { new UserRole { Role = UserRoleType.User } }
        };

        var pagedUsers = new PagedResult<User>(new List<User> { followerUser }, 1, 1, 10, 1);
        _followsMock.Setup(x => x.GetFollowersAsync(targetUserId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedUsers);

        _followsMock.Setup(x => x.GetFollowersCountAsync(followerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(15);

        var pagedTutorials = new PagedResult<Tutorial>(new List<Tutorial>(), 3, 1, 1, 3);
        _tutorialsMock.Setup(x => x.GetByAuthorAsync(followerUserId, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedTutorials);

        _followsMock.Setup(x => x.GetFollowAsync(currentUserId, followerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FollowRelationship());

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        
        var dto = result.Items.First();
        dto.UserId.Should().Be(followerUserId);
        dto.DisplayName.Should().Be("Follower");
        dto.FollowerCount.Should().Be(15);
        dto.TutorialCount.Should().Be(3);
        dto.IsFollowing.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_EmptyFollowers_ReturnsEmptyPagedResult()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var query = new GetFollowersQuery(targetUserId, null, 1, 10);

        var pagedUsers = new PagedResult<User>(new List<User>(), 0, 1, 10, 0);
        _followsMock.Setup(x => x.GetFollowersAsync(targetUserId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedUsers);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}
