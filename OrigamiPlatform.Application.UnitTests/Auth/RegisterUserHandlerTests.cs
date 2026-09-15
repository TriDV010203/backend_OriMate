using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.Commands.Auth;
using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;
using Xunit;

namespace OrigamiPlatform.Application.UnitTests.Auth;

/// <summary>
/// Unit tests for <see cref="RegisterUserHandler"/>.
/// Pattern: Arrange – Act – Assert (Given – When – Then).
/// Naming: [MethodName]_[Scenario]_[ExpectedResult]
/// </summary>
public class RegisterUserHandlerTests
{
    // ──────────────────────────────────────────────
    // Shared mocks & SUT
    // ──────────────────────────────────────────────
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasher> _hasherMock = new();
    private readonly Mock<ITokenService> _tokensMock = new();
    private readonly Mock<IEmailService> _emailMock = new();

    private RegisterUserHandler CreateSut() =>
        new(_userRepoMock.Object, _hasherMock.Object, _tokensMock.Object, _emailMock.Object);

    // ──────────────────────────────────────────────────────────────────
    // TC-01: HandleAsync_ValidCommand_ReturnsAuthResponse
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsAuthResponse()
    {
        // ── Arrange ──────────────────────────────────────────────────
        var command = new RegisterUserCommand(
            Email: "new@example.com",
            Password: "P@ssw0rd!",
            DisplayName: "Origami Fan");

        var jwtExpiry = DateTime.UtcNow.AddHours(1);
        var refreshExpiry = DateTime.UtcNow.AddDays(30);

        _userRepoMock
            .Setup(r => r.ExistsByEmailAsync(command.Email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _hasherMock
            .Setup(h => h.Hash(command.Password))
            .Returns("hashed-password");

        _tokensMock
            .Setup(t => t.GenerateRefreshToken())
            .Returns(("raw-refresh", "hashed-refresh", refreshExpiry));

        _tokensMock
            .Setup(t => t.GenerateToken(It.IsAny<Domain.Entities.User>()))
            .Returns(("jwt-token", jwtExpiry));

        _userRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Domain.Entities.User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _emailMock
            .Setup(e => e.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var result = await sut.HandleAsync(command);

        // ── Assert ────────────────────────────────────────────────────
        result.Should().NotBeNull();
        result.Email.Should().Be(command.Email.ToLowerInvariant());
        result.DisplayName.Should().Be(command.DisplayName);
        result.Token.Should().Be("jwt-token");
        result.RefreshToken.Should().Be("raw-refresh");
        result.ExpiresAt.Should().Be(jwtExpiry);
        result.Roles.Should().ContainSingle(r => r == "User");

        // Side-effects verified
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.User>(), It.IsAny<CancellationToken>()), Times.Once);
        _emailMock.Verify(e => e.SendVerificationEmailAsync(
            command.Email.ToLowerInvariant(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────
    // TC-02: HandleAsync_EmailAlreadyExists_ThrowsDomainException
    // ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_EmailAlreadyExists_ThrowsDomainException()
    {
        // ── Arrange ──────────────────────────────────────────────────
        var command = new RegisterUserCommand(
            Email: "existing@example.com",
            Password: "P@ssw0rd!",
            DisplayName: "Duplicate User");

        _userRepoMock
            .Setup(r => r.ExistsByEmailAsync(command.Email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────
        var act = () => sut.HandleAsync(command);

        // ── Assert ────────────────────────────────────────────────────
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*already registered*");

        // Ensure no user was persisted and no email was sent
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<Domain.Entities.User>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailMock.Verify(e => e.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
