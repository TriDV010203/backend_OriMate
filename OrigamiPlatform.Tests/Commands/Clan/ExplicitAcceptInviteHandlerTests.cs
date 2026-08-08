using Moq;
using OrigamiPlatform.Application.Commands.Clan;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.Tests.Commands.Clan;

public class ExplicitAcceptInviteHandlerTests
{
    private readonly Mock<IClanInviteRepository> _mockInvites;
    private readonly Mock<IClanMemberRepository> _mockMembers;
    private readonly AcceptInviteHandler _handler;

    public ExplicitAcceptInviteHandlerTests()
    {
        _mockInvites = new Mock<IClanInviteRepository>();
        _mockMembers = new Mock<IClanMemberRepository>();
        _handler = new AcceptInviteHandler(_mockInvites.Object, _mockMembers.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidInviteAndNotAMember_AddsMemberAndUpdatesInviteStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();
        var clanId = Guid.NewGuid();

        var command = new AcceptInviteCommand(userId, inviteId);
        
        var invite = new ClanInvite 
        { 
            Id = inviteId, 
            UserId = userId, 
            ClanId = clanId, 
            Status = ClanInviteStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        _mockInvites
            .Setup(x => x.GetByIdAsync(inviteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        _mockMembers
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClanMember?)null);

        _mockMembers
            .Setup(x => x.AddAsync(It.IsAny<ClanMember>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockInvites
            .Setup(x => x.UpdateAsync(It.IsAny<ClanInvite>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _mockMembers.Verify(x => x.AddAsync(It.Is<ClanMember>(m => 
            m.UserId == userId && 
            m.ClanId == clanId && 
            m.ContributionPoints == 0
        ), It.IsAny<CancellationToken>()), Times.Once);

        _mockInvites.Verify(x => x.UpdateAsync(It.Is<ClanInvite>(i => 
            i.Id == inviteId && 
            i.Status == ClanInviteStatus.Accepted
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
