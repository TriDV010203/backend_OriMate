using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Application.DTOs.AdminConfiguration;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;
using OrigamiPlatform.Application.Commands.AdminConfiguration;

namespace OrigamiPlatform.Application.UnitTests.Admin.Commands;

public class SuspendUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly Mock<INotificationService> _notificationServiceMock;

    public SuspendUserCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        _notificationServiceMock = new Mock<INotificationService>();
    }

    [Fact]
    public async Task HandleAsync_ActiveUser_UpdatesStatusToSuspended()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        
        var user = new User 
        { 
            Id = targetUserId, 
            Status = AccountStatus.Active,
            Roles = new List<UserRole> { new UserRole { Role = UserRoleType.User } } 
        };
        
        _userRepositoryMock.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var request = new SuspendUserRequest("Violation of community guidelines.");
        var command = new SuspendUserCommand(adminId, targetUserId, request);
        var handler = new SuspendUserHandler(_userRepositoryMock.Object, _auditLogRepositoryMock.Object, _notificationServiceMock.Object);

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        user.Status.Should().Be(AccountStatus.Suspended);
        _userRepositoryMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _auditLogRepositoryMock.Verify(x => x.LogAsync(It.Is<AuditLog>(l => l.Action == "SuspendAccount" && l.NewValue == request.Reason), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AdminCannotSuspendAnotherAdmin_ThrowsForbiddenException()
    {
        // Arrange
        var targetAdminId = Guid.NewGuid();
        var requestAdminId = Guid.NewGuid();
        
        var targetAdminUser = new User 
        { 
            Id = targetAdminId, 
            Status = AccountStatus.Active,
            Roles = new List<UserRole> { new UserRole { Role = UserRoleType.Admin } } 
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(targetAdminId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetAdminUser);

        var request = new SuspendUserRequest("Suspension reason.");
        var command = new SuspendUserCommand(requestAdminId, targetAdminId, request);
        var handler = new SuspendUserHandler(_userRepositoryMock.Object, _auditLogRepositoryMock.Object, _notificationServiceMock.Object);

        // Act
        Func<Task> act = async () => await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("*Admin*");
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
