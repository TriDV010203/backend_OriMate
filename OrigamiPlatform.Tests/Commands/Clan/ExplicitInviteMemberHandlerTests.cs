using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.Commands.Clan;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrigamiPlatform.Tests.Commands.Clan;

public class ExplicitInviteMemberHandlerTests
{
    private readonly Mock<IClanRepository> _clansMock;
    private readonly Mock<IClanMemberRepository> _membersMock;
    private readonly Mock<IClanInviteRepository> _invitesMock;
    private readonly InviteMemberHandler _handler;

    public ExplicitInviteMemberHandlerTests()
    {
        _clansMock = new Mock<IClanRepository>();
        _membersMock = new Mock<IClanMemberRepository>();
        _invitesMock = new Mock<IClanInviteRepository>();
        _handler = new InviteMemberHandler(_clansMock.Object, _membersMock.Object, _invitesMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_Success()
    {
        // Arrange
        var requesterId = Guid.NewGuid();
        var clanId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();

        var clan = new OrigamiPlatform.Domain.Entities.Clan { Id = clanId, OwnerId = requesterId };
        
        _clansMock.Setup(c => c.GetByIdAsync(clanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(clan);
            
        _membersMock.Setup(m => m.GetByUserIdAsync(inviteeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClanMember)null);

        var command = new InviteMemberCommand(requesterId, clanId, inviteeId);

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        _invitesMock.Verify(i => i.AddAsync(It.Is<ClanInvite>(inv =>
            inv.ClanId == clanId &&
            inv.UserId == inviteeId &&
            inv.Status == ClanInviteStatus.Pending
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
