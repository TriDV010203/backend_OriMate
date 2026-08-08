using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.Features.Tutorials.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Tutorials;

public class UpdateTutorialHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockTutorialRepo;
    private readonly Mock<IBlockedWordService> _mockBlockedWords;
    private readonly UpdateTutorialHandler _handler;

    public UpdateTutorialHandlerTests()
    {
        _mockTutorialRepo = new Mock<ITutorialRepository>();
        _mockBlockedWords = new Mock<IBlockedWordService>();
        _handler = new UpdateTutorialHandler(_mockTutorialRepo.Object, _mockBlockedWords.Object);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new UpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), req);

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync((Tutorial?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(command));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_AuthorMismatch_ThrowsForbiddenException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new UpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), req);
        var tutorial = new Tutorial { Id = command.TutorialId, AuthorId = Guid.NewGuid() }; // Different AuthorId

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _handler.HandleAsync(command));
        Assert.Contains("not the author", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidStatus_ThrowsDomainException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new UpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), req);
        var tutorial = new Tutorial { Id = command.TutorialId, AuthorId = command.AuthorId, Status = TutorialStatus.Published };

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Only draft or revision-required tutorials can be edited", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_UpdatesTutorialAndReplacesSteps()
    {
        var steps = new List<CreateTutorialStepRequest> { new(1, "Step 1", null) };
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", steps);
        var command = new UpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), req);
        var tutorial = new Tutorial { Id = command.TutorialId, AuthorId = command.AuthorId, Status = TutorialStatus.Draft, Steps = new List<TutorialStep>() };

        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockTutorialRepo.Setup(r => r.GetActiveCategoryAsync(req.CategoryId, default)).ReturnsAsync(new Category());

        var result = await _handler.HandleAsync(command);

        Assert.NotNull(result);
        Assert.Equal("Valid Title", result.Title);
        
        _mockTutorialRepo.Setup(r => r.DeleteStepsByTutorialIdAsync(command.TutorialId, default)).Returns(Task.CompletedTask);
        _mockTutorialRepo.Setup(r => r.UpdateAsync(tutorial, default)).Returns(Task.CompletedTask);
        _mockTutorialRepo.Setup(r => r.AddStepsAsync(It.Is<List<TutorialStep>>(s => s.Count == 1), default)).Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task HandleAsync_DescriptionInvalidLength_ThrowsDomainException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Short", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new UpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), req);
        var tutorial = new Tutorial { Id = command.TutorialId, AuthorId = command.AuthorId, Status = TutorialStatus.Draft };
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Description must be between 20 and 500 characters", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_DescriptionContainsBlockedWord_ThrowsDomainException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Bad description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new UpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), req);
        var tutorial = new Tutorial { Id = command.TutorialId, AuthorId = command.AuthorId, Status = TutorialStatus.Draft };
        
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Valid Title", default)).ReturnsAsync(false);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Bad description spanning 20 chars!", default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Description contains a blocked word", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidType_ThrowsDomainException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "InvalidType", "cover.jpg", null);
        var command = new UpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), req);
        var tutorial = new Tutorial { Id = command.TutorialId, AuthorId = command.AuthorId, Status = TutorialStatus.Draft };
        
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Invalid tutorial type", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidDifficulty_ThrowsDomainException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "InvalidDiff", "Free", "cover.jpg", null);
        var command = new UpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), req);
        var tutorial = new Tutorial { Id = command.TutorialId, AuthorId = command.AuthorId, Status = TutorialStatus.Draft };
        
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Invalid difficulty", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_VIPWithoutSettings_ThrowsDomainException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "VIP", "cover.jpg", null);
        var command = new UpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), req);
        var tutorial = new Tutorial { Id = command.TutorialId, AuthorId = command.AuthorId, Status = TutorialStatus.Draft };
        
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockTutorialRepo.Setup(r => r.GetActiveCreatorVipSettingsAsync(command.AuthorId, default)).ReturnsAsync((CreatorVipSettings?)null);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("You must configure a VIP pricing tier", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidCategory_ThrowsDomainException()
    {
        var req = new UpdateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 999, "Beginner", "Free", "cover.jpg", null);
        var command = new UpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), req);
        var tutorial = new Tutorial { Id = command.TutorialId, AuthorId = command.AuthorId, Status = TutorialStatus.Draft };
        
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
        var command = new UpdateTutorialCommand(Guid.NewGuid(), Guid.NewGuid(), req);
        var tutorial = new Tutorial { Id = command.TutorialId, AuthorId = command.AuthorId, Status = TutorialStatus.Draft };
        
        _mockTutorialRepo.Setup(r => r.GetByIdWithStepsAsync(command.TutorialId, default)).ReturnsAsync(tutorial);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Valid Title", default)).ReturnsAsync(false);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Valid description spanning 20 chars!", default)).ReturnsAsync(false);
        _mockTutorialRepo.Setup(r => r.GetActiveCategoryAsync(1, default)).ReturnsAsync(new Category());
        
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Bad Step", default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("description contains a blocked word", ex.Message);
    }
}
