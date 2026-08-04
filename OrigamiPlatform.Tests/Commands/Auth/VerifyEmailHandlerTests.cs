using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Auth;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Auth;

public class VerifyEmailHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly VerifyEmailHandler _handler;

    public VerifyEmailHandlerTests()
    {
        // 1. Arrange: Khởi tạo Mock Object
        _mockUserRepository = new Mock<IUserRepository>();

        // Truyền Mock Object vào Handler
        _handler = new VerifyEmailHandler(_mockUserRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_InvalidToken_ThrowsNotFoundException()
    {
        // Arrange
        var command = new VerifyEmailCommand("invalid-token");
        // Giả lập DB không tìm thấy user nào khớp với token này (trả về null)
        _mockUserRepository.Setup(repo => repo.GetByVerificationTokenAsync(command.Token, default))
                           .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.HandleAsync(command)
        );
        Assert.Equal("Invalid verification token.", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_TokenExpired_ThrowsDomainException()
    {
        // Arrange
        var command = new VerifyEmailCommand("expired-token");
        var user = new User { TokenExpiry = DateTime.UtcNow.AddMinutes(-10) }; // Đã hết hạn 10 phút trước

        _mockUserRepository.Setup(repo => repo.GetByVerificationTokenAsync(command.Token, default))
                           .ReturnsAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            _handler.HandleAsync(command)
        );
        Assert.Equal("Verification link has expired.", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_UserAlreadyVerified_ReturnsMessage()
    {
        // Arrange
        var command = new VerifyEmailCommand("valid-token");
        var user = new User
        {
            TokenExpiry = DateTime.UtcNow.AddMinutes(10), // Chưa hết hạn
            Status = AccountStatus.Active // Nhưng đã kích hoạt rồi
        };

        _mockUserRepository.Setup(repo => repo.GetByVerificationTokenAsync(command.Token, default))
                           .ReturnsAsync(user);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.Equal("Email already verified.", result.Message);
        // Đảm bảo hàm Update không bị gọi thừa
        _mockUserRepository.Verify(repo => repo.UpdateAsync(It.IsAny<User>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ValidToken_UpdatesUserAndReturnsSuccess()
    {
        // Arrange
        var command = new VerifyEmailCommand("valid-token");
        var user = new User
        {
            Status = AccountStatus.Unverified,
            VerificationToken = "valid-token",
            TokenExpiry = DateTime.UtcNow.AddMinutes(10)
        };

        _mockUserRepository.Setup(repo => repo.GetByVerificationTokenAsync(command.Token, default))
                           .ReturnsAsync(user);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.Equal("Email verified successfully.", result.Message);
        Assert.Equal(AccountStatus.Active, user.Status);
        Assert.Null(user.VerificationToken);
        Assert.Null(user.TokenExpiry);
        // Đảm bảo hàm UpdateAsync CÓ được gọi chính xác 1 lần
        _mockUserRepository.Verify(repo => repo.UpdateAsync(user, default), Times.Once);
    }
}
