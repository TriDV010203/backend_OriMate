using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.Commands.Auth;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;
using Xunit;

namespace OrigamiPlatform.Application.UnitTests.Auth;

/// <summary>
/// Unit tests for <see cref="LoginHandler"/>.
/// Pattern: Arrange – Act – Assert (Given – When – Then).
/// Naming: [MethodName]_[Scenario]_[ExpectedResult]
/// </summary>
public class LoginHandlerTests
{
    // ──────────────────────────────────────────────
    // Shared mocks & SUT
    // ──────────────────────────────────────────────
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();
    private readonly Mock<ITokenService> _tokensMock = new();

    private LoginHandler CreateSut() =>
        new(_userRepoMock.Object, _hasherMock.Object, _tokensMock.Object);

    /// <summary>
    /// Builds a minimal active <see cref="User"/> with the given password hash.
    /// </summary>
    private static User BuildActiveUser(string passwordHash = "correct-hash") => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@example.com",
        PasswordHash = passwordHash,
        Status = AccountStatus.Active,
        CreatedAt = DateTime.UtcNow,
        Profile = new UserProfile { DisplayName = "Test User", CreatedAt = DateTime.UtcNow },
        Roles = new List<UserRole>
        {
            new() { Role = UserRoleType.User, CreatedAt = DateTime.UtcNow }
        }
    };

    // ──────────────────────────────────────────────────────────────────
    // TC-01: HandleAsync_ValidCredentials_ReturnsAuthResponse
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_ValidCredentials_ReturnsAuthResponse()
    {
        // ── Arrange ──────────────────────────────────────────────────
        var command = new LoginCommand(Email: "user@example.com", Password: "correct-password");
        var activeUser = BuildActiveUser();
        var jwtExpiry = DateTime.UtcNow.AddHours(1);
        var refreshExpiry = DateTime.UtcNow.AddDays(30);

        _userRepoMock
            .Setup(r => r.GetByEmailAsync(command.Email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeUser);

        _hasherMock
            .Setup(h => h.Verify(command.Password, activeUser.PasswordHash))
            .Returns(true);

        _tokensMock
            .Setup(t => t.GenerateToken(activeUser))
            .Returns(("jwt-token", jwtExpiry));

        _tokensMock
            .Setup(t => t.GenerateRefreshToken())
            .Returns(("raw-refresh", "hashed-refresh", refreshExpiry));

        _userRepoMock
            .Setup(r => r.UpdateAsync(activeUser, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var result = await sut.HandleAsync(command);

        // ── Assert ────────────────────────────────────────────────────
        result.Should().NotBeNull();
        result.Email.Should().Be(activeUser.Email);
        result.Token.Should().Be("jwt-token");
        result.RefreshToken.Should().Be("raw-refresh");
        result.ExpiresAt.Should().Be(jwtExpiry);
        result.Roles.Should().ContainSingle(r => r == "User");

        // Verify refresh token is persisted
        _userRepoMock.Verify(r => r.UpdateAsync(activeUser, It.IsAny<CancellationToken>()), Times.Once);
        activeUser.RefreshTokenHash.Should().Be("hashed-refresh");
        activeUser.RefreshTokenExpiresAt.Should().Be(refreshExpiry);
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-02: HandleAsync_WrongPassword_ThrowsDomainException
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_WrongPassword_ThrowsDomainException()
    {
        // ── Arrange ──────────────────────────────────────────────────
        var command = new LoginCommand(Email: "user@example.com", Password: "wrong-password");
        var activeUser = BuildActiveUser();

        _userRepoMock
            .Setup(r => r.GetByEmailAsync(command.Email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeUser);

        _hasherMock
            .Setup(h => h.Verify(command.Password, activeUser.PasswordHash))
            .Returns(false); // <── password mismatch

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var act = () => sut.HandleAsync(command);

        // ── Assert ────────────────────────────────────────────────────
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Invalid email or password*");

        _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-03: HandleAsync_UserNotFound_ThrowsDomainException
    // (same public-facing message as wrong password – avoids user enumeration)
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_UserNotFound_ThrowsDomainException()
    {
        // ── Arrange ──────────────────────────────────────────────────
        var command = new LoginCommand(Email: "ghost@example.com", Password: "any-password");

        _userRepoMock
            .Setup(r => r.GetByEmailAsync(command.Email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null); // <── user does not exist

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var act = () => sut.HandleAsync(command);

        // ── Assert ────────────────────────────────────────────────────
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Invalid email or password*");
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-04: HandleAsync_AccountUnverified_ThrowsForbiddenException
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_AccountUnverified_ThrowsForbiddenException()
    {
        // ── Arrange ──────────────────────────────────────────────────
        var command = new LoginCommand(Email: "user@example.com", Password: "correct-password");
        var unverifiedUser = BuildActiveUser();
        unverifiedUser.Status = AccountStatus.Unverified; // <── not yet verified

        _userRepoMock
            .Setup(r => r.GetByEmailAsync(command.Email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(unverifiedUser);

        _hasherMock
            .Setup(h => h.Verify(command.Password, unverifiedUser.PasswordHash))
            .Returns(true);

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var act = () => sut.HandleAsync(command);

        // ── Assert ────────────────────────────────────────────────────
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*verify your email*");
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-05: HandleAsync_AccountSuspended_ThrowsForbiddenException
    // (maps to the "Account Locked / Suspended" business rule)
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_AccountSuspended_ThrowsForbiddenException()
    {
        // ── Arrange ──────────────────────────────────────────────────
        var command = new LoginCommand(Email: "user@example.com", Password: "correct-password");
        var suspendedUser = BuildActiveUser();
        suspendedUser.Status = AccountStatus.Suspended; // <── account locked/suspended

        _userRepoMock
            .Setup(r => r.GetByEmailAsync(command.Email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspendedUser);

        _hasherMock
            .Setup(h => h.Verify(command.Password, suspendedUser.PasswordHash))
            .Returns(true);

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var act = () => sut.HandleAsync(command);

        // ── Assert ────────────────────────────────────────────────────
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*suspended*");

        _userRepoMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
