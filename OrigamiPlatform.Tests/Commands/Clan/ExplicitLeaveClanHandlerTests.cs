using Moq;
using OrigamiPlatform.Application.Commands.Clan;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrigamiPlatform.Tests.Commands.Clan;

public class ExplicitLeaveClanHandlerTests
{
    private readonly Mock<IClanMemberRepository> _membersMock;
    private readonly LeaveClanHandler _handler;

    public ExplicitLeaveClanHandlerTests()
    {
        _membersMock = new Mock<IClanMemberRepository>();
        _handler = new LeaveClanHandler(_membersMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_DeletesMember()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var clanId = Guid.NewGuid();
        
        var clan = new OrigamiPlatform.Domain.Entities.Clan { Id = clanId, OwnerId = Guid.NewGuid() }; // Owner is not the user leaving
        var member = new ClanMember { UserId = userId, ClanId = clanId, Clan = clan };

        _membersMock.Setup(m => m.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var command = new LeaveClanCommand(userId, clanId);

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        _membersMock.Verify(m => m.DeleteAsync(member, It.IsAny<CancellationToken>()), Times.Once);
    }
}
