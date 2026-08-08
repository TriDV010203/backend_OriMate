using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.Commands.Clan;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrigamiPlatform.Tests.Commands.Clan;

public class ExplicitTransferOwnershipHandlerTests
{
    private readonly Mock<IClanRepository> _clansMock;
    private readonly Mock<IClanMemberRepository> _membersMock;
    private readonly TransferOwnershipHandler _handler;

    public ExplicitTransferOwnershipHandlerTests()
    {
        _clansMock = new Mock<IClanRepository>();
        _membersMock = new Mock<IClanMemberRepository>();
        _handler = new TransferOwnershipHandler(_clansMock.Object, _membersMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_TransfersOwnershipAndReturnsDto()
    {
        // Arrange
        var requesterId = Guid.NewGuid();
        var clanId = Guid.NewGuid();
        var newOwnerId = Guid.NewGuid();

        var clan = new OrigamiPlatform.Domain.Entities.Clan { Id = clanId, OwnerId = requesterId };
        var newOwnerMembership = new ClanMember { UserId = newOwnerId, ClanId = clanId, User = new User { Email = "test@example.com" } };
        var clanMembers = new List<ClanMember> { newOwnerMembership };

        _clansMock.Setup(c => c.GetByIdAsync(clanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(clan);

        _membersMock.Setup(m => m.GetByUserIdAsync(newOwnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newOwnerMembership);

        _membersMock.Setup(m => m.GetByClanIdAsync(clanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(clanMembers);

        var command = new TransferOwnershipCommand(requesterId, clanId, newOwnerId);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        clan.OwnerId.Should().Be(newOwnerId);
        _clansMock.Verify(c => c.UpdateAsync(clan, It.IsAny<CancellationToken>()), Times.Once);
        result.Should().NotBeNull();
        result.Id.Should().Be(clanId);
    }
}
