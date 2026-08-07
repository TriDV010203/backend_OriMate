using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Tutorials;

public class SubmitTutorialHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockRepo;
    private readonly Mock<INotificationService> _mockNotifications;
    private readonly SubmitTutorialHandler _handler;

    public SubmitTutorialHandlerTests()
    {
        _mockRepo = new Mock<ITutorialRepository>();
        _mockNotifications = new Mock<INotificationService>();
        _handler = new SubmitTutorialHandler(_mockRepo.Object, _mockNotifications.Object);
    }

    [Fact]
    public async Task HandleAsync_AuthorMismatch_ThrowsForbiddenException()
    {
        var command = new SubmitTutorialCommand(Guid.NewGuid(), Guid.NewGuid());
        var tutorial = new Tutorial { Id = command.TutorialId, AuthorId = Guid.NewGuid() }; // Different AuthorId
        _mockRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _handler.HandleAsync(command));
        Assert.Equal("You are not the author of this tutorial.", ex.Message);
    }

    [Theory]
    [InlineData(TutorialStatus.Published)]
    [InlineData(TutorialStatus.PendingManagerReview)]
    [InlineData(TutorialStatus.Removed)]
    public async Task HandleAsync_InvalidStatus_ThrowsDomainException(TutorialStatus status)
    {
        var authorId = Guid.NewGuid();
        var command = new SubmitTutorialCommand(Guid.NewGuid(), authorId);
        var tutorial = new Tutorial { Id = command.TutorialId, AuthorId = authorId, Status = status };

        _mockRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Equal("Tutorial cannot be submitted in its current status.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_AccumulatesValidationErrors_ThrowsDomainException()
    {
        var authorId = Guid.NewGuid();
        var command = new SubmitTutorialCommand(Guid.NewGuid(), authorId);
        var tutorial = new Tutorial
        {
            Id = command.TutorialId,
            AuthorId = authorId,
            Status = TutorialStatus.Draft,
            Title = "1234", // Too short (< 5)
            Description = "Too short", // Too short (< 20)
            CoverImageUrl = "", // Missing
            CategoryId = 99,
            Steps = new List<TutorialStep> { new TutorialStep(), new TutorialStep() } // Only 2 steps (< 3)
        };

        _mockRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);
        _mockRepo.Setup(r => r.GetActiveCategoryAsync(99, default)).ReturnsAsync((Category?)null);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));

        Assert.Contains("Title must be between 5 and 150 characters.", ex.Message);
        Assert.Contains("Description must be between 20 and 500 characters.", ex.Message);
        Assert.Contains("Cover image is required.", ex.Message);
        Assert.Contains("Category does not exist or is not active.", ex.Message);
        Assert.Contains("Tutorial must have between 3 and 30 steps", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidTutorial_SubmitsSuccessfully()
    {
        var authorId = Guid.NewGuid();
        var command = new SubmitTutorialCommand(Guid.NewGuid(), authorId);
        var tutorial = new Tutorial
        {
            Id = command.TutorialId,
            AuthorId = authorId,
            Status = TutorialStatus.Draft,
            Title = "Valid Title Origami",
            Description = "This is a valid description that has more than twenty characters.",
            CoverImageUrl = "http://example.com/cover.jpg",
            CategoryId = 1,
            Steps = new List<TutorialStep>
            {
                new TutorialStep { StepOrder = 1, Description = "Step 1", ImageUrl = "img1.jpg" },
                new TutorialStep { StepOrder = 2, Description = "Step 2", ImageUrl = "img2.jpg" },
                new TutorialStep { StepOrder = 3, Description = "Step 3", ImageUrl = "img3.jpg" }
            }
        };

        _mockRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);
        _mockRepo.Setup(r => r.GetActiveCategoryAsync(1, default)).ReturnsAsync(new Category());

        var response = await _handler.HandleAsync(command);

        Assert.NotNull(response);
        Assert.Equal(TutorialStatus.PendingManagerReview, tutorial.Status);

        _mockRepo.Verify(r => r.UpdateAsync(tutorial, default), Times.Once);
        _mockRepo.Verify(r => r.AddReviewHistoryAsync(It.IsAny<TutorialReviewHistory>(), default), Times.Once);
        _mockNotifications.Verify(n => n.NotifyUsersWithRoleAsync(
            UserRoleType.Manager,
            NotificationType.TutorialReadyForManagerApproval,
            It.IsAny<string>(),
            "Tutorial",
            tutorial.Id,
            default), Times.Once);
    }
}
