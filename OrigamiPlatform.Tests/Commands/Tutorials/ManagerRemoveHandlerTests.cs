using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.Features.Tutorials.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Tutorials;

public class ManagerRemoveHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockTutorialRepo;
    private readonly Mock<INotificationService> _mockNotifications;
    private readonly ManagerRemoveHandler _handler;

    public ManagerRemoveHandlerTests()
    {
        _mockTutorialRepo = new Mock<ITutorialRepository>();
        _mockNotifications = new Mock<INotificationService>();
        _handler = new ManagerRemoveHandler(_mockTutorialRepo.Object, _mockNotifications.Object);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        var command = new ManagerRemoveCommand(Guid.NewGuid(), Guid.NewGuid(), null);
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync((Tutorial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidStatus_ThrowsDomainException()
    {
        var command = new ManagerRemoveCommand(Guid.NewGuid(), Guid.NewGuid(), null);
        var tutorial = new Tutorial { Id = command.TutorialId, Status = TutorialStatus.Draft };
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Only published tutorials can be removed", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_RemovesTutorial()
    {
        var req = new ManagerRemoveRequest("Violates terms of service.");
        var command = new ManagerRemoveCommand(Guid.NewGuid(), Guid.NewGuid(), req);
        var tutorial = new Tutorial { Id = command.TutorialId, Status = TutorialStatus.Published, AuthorId = Guid.NewGuid() };

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);

        await _handler.HandleAsync(command);

        Assert.Equal(TutorialStatus.Removed, tutorial.Status);
        _mockTutorialRepo.Verify(r => r.UpdateAsync(tutorial, default), Times.Once);
        _mockTutorialRepo.Verify(r => r.AddReviewHistoryAsync(It.Is<TutorialReviewHistory>(h => 
            h.Action == "Remove" && 
            h.ToStatus == TutorialStatus.Removed &&
            h.Reason == req.Reason), default), Times.Once);
        _mockNotifications.Verify(n => n.NotifyUserAsync(tutorial.AuthorId, NotificationType.TutorialRemoved, It.IsAny<string>(), It.IsAny<string>(), tutorial.Id, default), Times.Once);
    }
}
