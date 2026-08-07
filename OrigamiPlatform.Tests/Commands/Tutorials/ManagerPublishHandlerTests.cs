using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Tutorials;

public class ManagerPublishHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockRepo;
    private readonly Mock<INotificationService> _mockNotifications;
    private readonly ManagerPublishHandler _handler;

    public ManagerPublishHandlerTests()
    {
        _mockRepo = new Mock<ITutorialRepository>();
        _mockNotifications = new Mock<INotificationService>();
        _handler = new ManagerPublishHandler(_mockRepo.Object, _mockNotifications.Object);
    }

    [Fact]
    public async Task HandleAsync_TutorialNotFound_ThrowsNotFoundException()
    {
        var command = new ManagerPublishCommand(Guid.NewGuid(), Guid.NewGuid());
        _mockRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync((Tutorial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Contains("not found", ex.Message);
    }

    [Theory]
    [InlineData(TutorialStatus.Draft)]
    [InlineData(TutorialStatus.RevisionRequired)]
    [InlineData(TutorialStatus.Published)]
    [InlineData(TutorialStatus.Removed)]
    public async Task HandleAsync_StatusNotPendingManagerReview_ThrowsDomainException(TutorialStatus invalidStatus)
    {
        var command = new ManagerPublishCommand(Guid.NewGuid(), Guid.NewGuid());
        var tutorial = new Tutorial { Id = command.TutorialId, Status = invalidStatus };

        _mockRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("Only tutorials pending manager review can be published.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_PublishesTutorialAndNotifiesAuthor()
    {
        var command = new ManagerPublishCommand(Guid.NewGuid(), Guid.NewGuid());
        var tutorial = new Tutorial
        {
            Id = command.TutorialId,
            AuthorId = Guid.NewGuid(),
            Status = TutorialStatus.PendingManagerReview
        };

        _mockRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);

        await _handler.HandleAsync(command);

        // Assert that the tutorial properties were updated
        Assert.Equal(TutorialStatus.Published, tutorial.Status);
        Assert.NotNull(tutorial.PublishedAt);

        // Assert Repository was called to save the changes
        _mockRepo.Verify(r => r.UpdateAsync(tutorial, default), Times.Once);

        // Assert Review History was inserted
        _mockRepo.Verify(r => r.AddReviewHistoryAsync(It.Is<TutorialReviewHistory>(h =>
            h.TutorialId == tutorial.Id &&
            h.ReviewerId == command.ManagerId &&
            h.ToStatus == TutorialStatus.Published &&
            h.Action == "Publish"
        ), default), Times.Once);

        // Assert Notification was sent to the author
        _mockNotifications.Verify(n => n.NotifyUserAsync(
            tutorial.AuthorId,
            NotificationType.TutorialPublished,
            It.IsAny<string>(),
            "Tutorial",
            tutorial.Id,
            default), Times.Once);
    }
}
