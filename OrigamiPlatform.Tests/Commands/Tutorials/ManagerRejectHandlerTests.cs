using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.Features.Tutorials.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Tutorials;

public class ManagerRejectHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockTutorialRepo;
    private readonly Mock<INotificationService> _mockNotifications;
    private readonly ManagerRejectHandler _handler;

    public ManagerRejectHandlerTests()
    {
        _mockTutorialRepo = new Mock<ITutorialRepository>();
        _mockNotifications = new Mock<INotificationService>();
        _handler = new ManagerRejectHandler(_mockTutorialRepo.Object, _mockNotifications.Object);
    }

    [Fact]
    public async Task HandleAsync_ShortReason_ThrowsDomainException()
    {
        var req = new ManagerRejectRequest("Short");
        var command = new ManagerRejectCommand(Guid.NewGuid(), Guid.NewGuid(), req);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("reason must be at least 10 characters", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        var req = new ManagerRejectRequest("This is a valid rejection reason.");
        var command = new ManagerRejectCommand(Guid.NewGuid(), Guid.NewGuid(), req);

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync((Tutorial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidStatus_ThrowsDomainException()
    {
        var req = new ManagerRejectRequest("This is a valid rejection reason.");
        var command = new ManagerRejectCommand(Guid.NewGuid(), Guid.NewGuid(), req);
        var tutorial = new Tutorial { Id = command.TutorialId, Status = TutorialStatus.Draft };

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Only tutorials pending manager review or already published", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_RejectsTutorial()
    {
        var req = new ManagerRejectRequest("This is a valid rejection reason.");
        var command = new ManagerRejectCommand(Guid.NewGuid(), Guid.NewGuid(), req);
        var tutorial = new Tutorial { Id = command.TutorialId, Status = TutorialStatus.PendingManagerReview, AuthorId = Guid.NewGuid() };

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);

        await _handler.HandleAsync(command);

        Assert.Equal(TutorialStatus.RevisionRequired, tutorial.Status);
        _mockTutorialRepo.Verify(r => r.UpdateAsync(tutorial, default), Times.Once);
        _mockTutorialRepo.Verify(r => r.AddReviewHistoryAsync(It.Is<TutorialReviewHistory>(h => 
            h.Action == "Reject" && 
            h.ToStatus == TutorialStatus.RevisionRequired &&
            h.Reason == req.Reason), default), Times.Once);
        _mockNotifications.Verify(n => n.NotifyUserAsync(tutorial.AuthorId, NotificationType.TutorialRejected, It.IsAny<string>(), It.IsAny<string>(), tutorial.Id, default), Times.Once);
    }
}
