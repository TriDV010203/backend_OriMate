using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Auth;
using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Auth;

public class LoginHandlerTests
{
    private readonly Mock<IUserRepository> _mockUsers;
    private readonly Mock<IPasswordHasher> _mockHasher;
    private readonly Mock<ITokenService> _mockTokens;
    private readonly LoginHandler _handler;

    public LoginHandlerTests()
    {
        _mockUsers = new Mock<IUserRepository>();
        _mockHasher = new Mock<IPasswordHasher>();
        _mockTokens = new Mock<ITokenService>();
        _handler = new LoginHandler(_mockUsers.Object, _mockHasher.Object, _mockTokens.Object);
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ThrowsDomainException()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "pass123");
        _mockUsers.Setup(u => u.GetByEmailAsync("test@example.com", default))
                  .ReturnsAsync((User?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("Invalid email or password.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidPassword_ThrowsDomainException()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "wrongpass");
        var user = new User { PasswordHash = "hashed" };
        _mockUsers.Setup(u => u.GetByEmailAsync("test@example.com", default)).ReturnsAsync(user);
        _mockHasher.Setup(h => h.Verify("wrongpass", "hashed")).Returns(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("Invalid email or password.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_UnverifiedUser_ThrowsForbiddenException()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "pass123");
        var user = new User { PasswordHash = "hashed", Status = AccountStatus.Unverified };

        _mockUsers.Setup(u => u.GetByEmailAsync("test@example.com", default)).ReturnsAsync(user);
        _mockHasher.Setup(h => h.Verify("pass123", "hashed")).Returns(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _handler.HandleAsync(command));
        Assert.Equal("Please verify your email before logging in.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_SuspendedUser_ThrowsForbiddenException()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "pass123");
        var user = new User { PasswordHash = "hashed", Status = AccountStatus.Suspended };

        _mockUsers.Setup(u => u.GetByEmailAsync("test@example.com", default)).ReturnsAsync(user);
        _mockHasher.Setup(h => h.Verify("pass123", "hashed")).Returns(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _handler.HandleAsync(command));
        Assert.Equal("Your account has been suspended.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "pass123");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hashed",
            Status = AccountStatus.Active,
            Profile = new UserProfile { DisplayName = "Test User" }
        };
        user.Roles.Add(new UserRole { Role = UserRoleType.User });

        _mockUsers.Setup(u => u.GetByEmailAsync("test@example.com", default)).ReturnsAsync(user);
        _mockHasher.Setup(h => h.Verify("pass123", "hashed")).Returns(true);

        var tokenExpiry = DateTime.UtcNow.AddHours(1);
        var refreshExpiry = DateTime.UtcNow.AddDays(7);
        _mockTokens.Setup(t => t.GenerateToken(user)).Returns(("jwt-token", tokenExpiry));
        _mockTokens.Setup(t => t.GenerateRefreshToken()).Returns(("raw-refresh", "hashed-refresh", refreshExpiry));

        // Act
        var response = await _handler.HandleAsync(command);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("jwt-token", response.Token);
        Assert.Equal("raw-refresh", response.RefreshToken);
        Assert.Equal(user.Id, response.UserId);

        Assert.Equal("hashed-refresh", user.RefreshTokenHash);
        Assert.Equal(refreshExpiry, user.RefreshTokenExpiresAt);

        _mockUsers.Verify(u => u.UpdateAsync(user, default), Times.Once);
    }
}
