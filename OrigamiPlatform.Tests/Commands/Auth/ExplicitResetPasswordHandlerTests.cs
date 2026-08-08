using Moq;
using OrigamiPlatform.Application.Commands.Auth;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrigamiPlatform.Tests.Commands.Auth
{
    public class ExplicitResetPasswordHandlerTests
    {
        private readonly Mock<IUserRepository> _usersMock;
        private readonly Mock<IPasswordHasher> _hasherMock;
        private readonly ResetPasswordHandler _handler;

        public ExplicitResetPasswordHandlerTests()
        {
            _usersMock = new Mock<IUserRepository>();
            _hasherMock = new Mock<IPasswordHasher>();
            _handler = new ResetPasswordHandler(_usersMock.Object, _hasherMock.Object);
        }

        [Fact]
        public async Task HandleAsync_ValidRequest_Success()
        {
            // Arrange
            var token = "validResetToken";
            var command = new ResetPasswordCommand(token, "newPassword123!", "newPassword123!");
            
            var user = new User
            {
                Id = Guid.NewGuid(),
                PasswordResetToken = token,
                PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1)
            };

            _usersMock.Setup(x => x.GetByPasswordResetTokenAsync(token, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _hasherMock.Setup(x => x.Hash("newPassword123!"))
                .Returns("hashedNewPassword");

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Password reset successful. You can now log in.", result.Message);
            
            Assert.Equal("hashedNewPassword", user.PasswordHash);
            Assert.Null(user.PasswordResetToken);
            Assert.Null(user.PasswordResetTokenExpiry);
            
            _usersMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
