using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.TutorialProgress;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.TutorialProgress;

public class UncompleteTutorialStepHandlerTests
{
    private readonly Mock<ITutorialStepProgressRepository> _mockProgress;
    private readonly UncompleteTutorialStepHandler _handler;

    public UncompleteTutorialStepHandlerTests()
    {
        _mockProgress = new Mock<ITutorialStepProgressRepository>();
        _handler = new UncompleteTutorialStepHandler(_mockProgress.Object);
    }

    [Fact]
    public async Task HandleAsync_ProgressNotFound_ThrowsNotFoundException()
    {
        var command = new UncompleteTutorialStepCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _mockProgress.Setup(p => p.GetAsync(command.UserId, command.StepId, default)).ReturnsAsync((TutorialStepProgress?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Equal("This step is not marked as completed.", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_RemovesProgressAndReturnsDto()
    {
        var command = new UncompleteTutorialStepCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var existingProgress = new TutorialStepProgress { UserId = command.UserId, TutorialStepId = command.StepId };
        var completedIds = new List<Guid> { Guid.NewGuid() }; // User still has one other step completed

        _mockProgress.Setup(p => p.GetAsync(command.UserId, command.StepId, default)).ReturnsAsync(existingProgress);
        _mockProgress.Setup(p => p.CountStepsAsync(command.TutorialId, default)).ReturnsAsync(5); // Tutorial has 5 steps total
        _mockProgress.Setup(p => p.GetCompletedStepIdsAsync(command.UserId, command.TutorialId, default)).ReturnsAsync(completedIds);

        var result = await _handler.HandleAsync(command);

        // Verify existing progress was removed
        _mockProgress.Verify(p => p.RemoveAsync(existingProgress, default), Times.Once);

        // Verify DTO fields
        Assert.Equal(command.TutorialId, result.TutorialId);
        Assert.Equal(5, result.TotalSteps);
        Assert.Equal(1, result.CompletedSteps);
        Assert.Equal(completedIds, result.CompletedStepIds);
    }
}
