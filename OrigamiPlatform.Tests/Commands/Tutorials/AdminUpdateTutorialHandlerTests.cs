using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.Features.Tutorials.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Tutorials;

public class AdminUpdateTutorialHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockTutorialRepo;
    private readonly Mock<IBlockedWordService> _mockBlockedWords;
    private readonly AdminUpdateTutorialHandler _handler;

    public AdminUpdateTutorialHandlerTests()
    {
        _mockTutorialRepo = new Mock<ITutorialRepository>();
        _mockBlockedWords = new Mock<IBlockedWordService>();
        _handler = new AdminUpdateTutorialHandler(_mockTutorialRepo.Object, _mockBlockedWords.Object);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new AdminUpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), UserRoleType.Admin, req);

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync((Tutorial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_NotOfficial_ThrowsForbiddenException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new AdminUpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), UserRoleType.Admin, req);
        var tutorial = new Tutorial { Id = command.TutorialId, IsOfficial = false };
        
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _handler.HandleAsync(command));
        Assert.Contains("Chỉ có thể chỉnh sửa nội dung bài viết chính thức", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_IsWorkingCopy_ThrowsDomainException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new AdminUpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), UserRoleType.Admin, req);
        var tutorial = new Tutorial { Id = command.TutorialId, IsOfficial = true, ParentTutorialId = Guid.NewGuid() };
        
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Cannot directly edit a review working copy", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_UpdatesTutorial()
    {
        var steps = new List<CreateTutorialStepRequest> { new(1, "Step 1", "img.jpg") };
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", steps);
        var command = new AdminUpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), UserRoleType.Admin, req);
        var tutorial = new Tutorial { Id = command.TutorialId, IsOfficial = true, Status = TutorialStatus.Published, Steps = new List<TutorialStep>() };

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockTutorialRepo.Setup(r => r.GetActiveCategoryAsync(req.CategoryId, default)).ReturnsAsync(new Category());

        var result = await _handler.HandleAsync(command);

        Assert.NotNull(result);
        Assert.Equal("Valid Title", result.Title);

        _mockTutorialRepo.Verify(r => r.DeleteStepsByTutorialIdAsync(command.TutorialId, default), Times.Once);
        _mockTutorialRepo.Verify(r => r.UpdateAsync(tutorial, default), Times.Once);
        _mockTutorialRepo.Verify(r => r.AddStepsAsync(It.Is<List<TutorialStep>>(s => s.Count == 1), default), Times.Once);

        _mockTutorialRepo.Verify(r => r.AddReviewHistoryAsync(It.Is<TutorialReviewHistory>(h => 
            h.Action == "AdminDirectEdit" && 
            h.ToStatus == TutorialStatus.Published), default), Times.Once);
    }
}
