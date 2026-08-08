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

    [Fact]
    public async Task HandleAsync_TitleInvalidLength_ThrowsDomainException()
    {
        var req = new UpdateTutorialRequest("A", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new AdminUpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), UserRoleType.Admin, req);
        var tutorial = new Tutorial { Id = command.TutorialId, IsOfficial = true };
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Title must be between 5 and 150 characters", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_DescriptionInvalidLength_ThrowsDomainException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Short", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new AdminUpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), UserRoleType.Admin, req);
        var tutorial = new Tutorial { Id = command.TutorialId, IsOfficial = true };
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Description must be between 20 and 500 characters", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_TitleContainsBlockedWord_ThrowsDomainException()
    {
        var req = new UpdateTutorialRequest("Bad Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new AdminUpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), UserRoleType.Admin, req);
        var tutorial = new Tutorial { Id = command.TutorialId, IsOfficial = true };
        
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Bad Title", default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Title contains a blocked word", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_DescriptionContainsBlockedWord_ThrowsDomainException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Bad Description here that is long enough", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new AdminUpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), UserRoleType.Admin, req);
        var tutorial = new Tutorial { Id = command.TutorialId, IsOfficial = true };
        
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Valid Title", default)).ReturnsAsync(false);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Bad Description here that is long enough", default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Description contains a blocked word", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidType_ThrowsDomainException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "InvalidType", "cover.jpg", null);
        var command = new AdminUpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), UserRoleType.Admin, req);
        var tutorial = new Tutorial { Id = command.TutorialId, IsOfficial = true };
        
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Invalid tutorial type", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidDifficulty_ThrowsDomainException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "InvalidDiff", "Free", "cover.jpg", null);
        var command = new AdminUpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), UserRoleType.Admin, req);
        var tutorial = new Tutorial { Id = command.TutorialId, IsOfficial = true };
        
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Invalid difficulty", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_VIPWithoutSettings_ThrowsDomainException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "VIP", "cover.jpg", null);
        var command = new AdminUpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), UserRoleType.Admin, req);
        var tutorial = new Tutorial { Id = command.TutorialId, IsOfficial = true, AuthorId = Guid.NewGuid() };
        
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockTutorialRepo.Setup(r => r.GetActiveCreatorVipSettingsAsync(tutorial.AuthorId, default)).ReturnsAsync((CreatorVipSettings?)null);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("must have a VIP pricing tier configured", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_CategoryNotActive_ThrowsDomainException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 999, "Beginner", "Free", "cover.jpg", null);
        var command = new AdminUpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), UserRoleType.Admin, req);
        var tutorial = new Tutorial { Id = command.TutorialId, IsOfficial = true };
        
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockTutorialRepo.Setup(r => r.GetActiveCategoryAsync(999, default)).ReturnsAsync((Category?)null);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("does not exist or is not active", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_StepDescriptionContainsBlockedWord_ThrowsDomainException()
    {
        var steps = new List<CreateTutorialStepRequest> { new(1, "Bad Step", "img.jpg") };
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", steps);
        var command = new AdminUpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), UserRoleType.Admin, req);
        var tutorial = new Tutorial { Id = command.TutorialId, IsOfficial = true };

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Valid Title", default)).ReturnsAsync(false);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Valid description spanning 20 chars!", default)).ReturnsAsync(false);
        _mockTutorialRepo.Setup(r => r.GetActiveCategoryAsync(req.CategoryId, default)).ReturnsAsync(new Category());
        
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Bad Step", default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("description contains a blocked word", ex.Message);
    }
}
