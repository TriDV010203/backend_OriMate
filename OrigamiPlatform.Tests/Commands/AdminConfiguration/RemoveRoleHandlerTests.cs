using Moq;
using OrigamiPlatform.Application.Commands.AdminConfiguration;
using OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.AdminConfiguration;

public class RemoveRoleHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IAuditLogRepository> _auditLogRepoMock = new();

    private RemoveRoleHandler CreateHandler()
        => new(_userRepoMock.Object, _auditLogRepoMock.Object);

    [Fact]
    public async Task HandleAsync_ValidRequest_RemovesRoleAndLogsAudit()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new RemoveRoleRequest("Manager");
        var command = new RemoveRoleCommand(actorId, userId, request);

        var existingUser = new User
        {
            Id = userId,
            Roles = new List<UserRole> { new UserRole { Role = UserRoleType.User }, new UserRole { Role = UserRoleType.Manager } }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(existingUser);

        _userRepoMock.Setup(r => r.RemoveRoleAsync(userId, UserRoleType.Manager, default))
            .Returns(Task.CompletedTask);

        _auditLogRepoMock.Setup(r => r.LogAsync(It.IsAny<AuditLog>(), default))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.HandleAsync(command);

        // Assert
        _userRepoMock.Verify(r => r.RemoveRoleAsync(userId, UserRoleType.Manager, default), Times.Once);
        _auditLogRepoMock.Verify(r => r.LogAsync(It.Is<AuditLog>(l => 
            l.ActorId == actorId && 
            l.Action == "RemoveRole" && 
            l.EntityType == "User" && 
            l.EntityId == userId.ToString() && 
            l.OldValue == "Manager" && 
            l.NewValue == null), default), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_EmptyRole_ThrowsDomainException(string? invalidRole)
    {
        // Arrange
        var command = new RemoveRoleCommand(Guid.NewGuid(), Guid.NewGuid(), new RemoveRoleRequest(invalidRole!));
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(command));
        Assert.Equal("Role is required.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidRole_ThrowsDomainException()
    {
        // Arrange
        var command = new RemoveRoleCommand(Guid.NewGuid(), Guid.NewGuid(), new RemoveRoleRequest("SuperAdmin123"));
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => handler.HandleAsync(command));
        Assert.Equal("Invalid role: SuperAdmin123.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_RemoveUserRole_ThrowsBadRequestException()
    {
        // Arrange
        var command = new RemoveRoleCommand(Guid.NewGuid(), Guid.NewGuid(), new RemoveRoleRequest("User"));
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => handler.HandleAsync(command));
        Assert.Equal("Cannot remove the base User role.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_RemoveOwnAdminRole_ThrowsBadRequestException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new RemoveRoleCommand(userId, userId, new RemoveRoleRequest("Admin"));
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => handler.HandleAsync(command));
        Assert.Equal("Cannot remove your own Admin role.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new RemoveRoleCommand(Guid.NewGuid(), userId, new RemoveRoleRequest("Manager"));

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command));
        Assert.Equal($"User {userId} not found.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_UserDoesNotHaveRole_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new RemoveRoleCommand(Guid.NewGuid(), userId, new RemoveRoleRequest("Manager"));

        var existingUser = new User
        {
            Id = userId,
            Roles = new List<UserRole> { new UserRole { Role = UserRoleType.User } }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(existingUser);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.HandleAsync(command));
        Assert.Equal("User does not have the Manager role.", ex.Message);
    }
}
