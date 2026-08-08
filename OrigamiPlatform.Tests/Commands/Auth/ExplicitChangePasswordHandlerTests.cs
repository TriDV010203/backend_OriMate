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
    public class ExplicitChangePasswordHandlerTests
    {
        private readonly Mock<IUserRepository> _usersMock;
        private readonly Mock<IPasswordHasher> _hasherMock;
        private readonly ChangePasswordHandler _handler;

        public ExplicitChangePasswordHandlerTests()
        {
            _usersMock = new Mock<IUserRepository>();
            _hasherMock = new Mock<IPasswordHasher>();
            _handler = new ChangePasswordHandler(_usersMock.Object, _hasherMock.Object);
        }

        [Fact]
        public async Task HandleAsync_ValidRequest_Success()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new ChangePasswordCommand(userId, "oldPassword123!", "newPassword123!", "newPassword123!");
            
            var user = new User
            {
                Id = userId,
                PasswordHash = "hashedOldPassword"
            };

            _usersMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _hasherMock.Setup(x => x.Verify("oldPassword123!", "hashedOldPassword"))
                .Returns(true);

            _hasherMock.Setup(x => x.Hash("newPassword123!"))
                .Returns("hashedNewPassword");

            // Act
            var result = await _handler.HandleAsync(command);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Password changed successfully. Please log in again.", result.Message);
            
            Assert.Equal("hashedNewPassword", user.PasswordHash);
            Assert.Null(user.RefreshTokenHash);
            Assert.Null(user.RefreshTokenExpiresAt);
            
            _usersMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
