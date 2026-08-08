using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.Features.Tutorials.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Constants;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Tutorials;

public class AdminCreateTutorialHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockTutorialRepo;
    private readonly Mock<IBlockedWordService> _mockBlockedWords;
    private readonly AdminCreateTutorialHandler _handler;

    public AdminCreateTutorialHandlerTests()
    {
        _mockTutorialRepo = new Mock<ITutorialRepository>();
        _mockBlockedWords = new Mock<IBlockedWordService>();
        _handler = new AdminCreateTutorialHandler(_mockTutorialRepo.Object, _mockBlockedWords.Object);
    }

    [Fact]
    public async Task HandleAsync_InvalidLength_ThrowsDomainException()
    {
        var req = new CreateTutorialRequest("A", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new AdminCreateTutorialCommand(Guid.NewGuid(), UserRoleType.Admin, req);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Title must be between 5 and 150 characters", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_MissingCoverImage_ThrowsDomainException()
    {
        var req = new CreateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "", null);
        var command = new AdminCreateTutorialCommand(Guid.NewGuid(), UserRoleType.Admin, req);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("cover image is required", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_NotEnoughSteps_ThrowsDomainException()
    {
        var steps = new List<CreateTutorialStepRequest> { new(1, "Step 1", "img.jpg") };
        var req = new CreateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", steps);
        var command = new AdminCreateTutorialCommand(Guid.NewGuid(), UserRoleType.Admin, req);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Need 3 to 30 steps", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_CreatesOfficialTutorial()
    {
        var steps = new List<CreateTutorialStepRequest> 
        {
            new(1, "Step 1", "img.jpg"),
            new(2, "Step 2", "img2.jpg"),
            new(3, "Step 3", "img3.jpg")
        };
        var req = new CreateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", steps);
        var command = new AdminCreateTutorialCommand(Guid.NewGuid(), UserRoleType.Admin, req);

        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockTutorialRepo.Setup(r => r.GetActiveCategoryAsync(req.CategoryId, default)).ReturnsAsync(new Category());
        _mockTutorialRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), default)).ReturnsAsync(false);

        var result = await _handler.HandleAsync(command);

        Assert.NotNull(result);
        Assert.Equal("Valid Title", result.Title);

        _mockTutorialRepo.Verify(r => r.AddAsync(It.Is<Tutorial>(t => 
            t.IsOfficial &&
            t.Status == TutorialStatus.Published &&
            t.AuthorId == SystemUsers.OfficialTutorialAuthorId &&
            t.Type == TutorialType.Free &&
            t.Steps.Count == 3), default), Times.Once);

        _mockTutorialRepo.Verify(r => r.AddReviewHistoryAsync(It.Is<TutorialReviewHistory>(h => 
            h.Action == "AdminAutoPublish" && 
            h.ToStatus == TutorialStatus.Published), default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_TitleContainsBlockedWord_ThrowsDomainException()
    {
        var req = new CreateTutorialRequest("Bad Word Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", new List<CreateTutorialStepRequest> { new(1, "S", "i"), new(2, "S", "i"), new(3, "S", "i") });
        var command = new AdminCreateTutorialCommand(Guid.NewGuid(), UserRoleType.Admin, req);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Bad Word Title", default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Title contains a blocked word", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidDifficulty_ThrowsDomainException()
    {
        var steps = new List<CreateTutorialStepRequest> { new(1, "Step 1", "img.jpg"), new(2, "Step 2", "img2.jpg"), new(3, "Step 3", "img3.jpg") };
        var req = new CreateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "SuperHard", "Free", "cover.jpg", steps);
        var command = new AdminCreateTutorialCommand(Guid.NewGuid(), UserRoleType.Admin, req);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Invalid difficulty", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_CategoryNotActive_ThrowsDomainException()
    {
        var steps = new List<CreateTutorialStepRequest> { new(1, "Step 1", "img.jpg"), new(2, "Step 2", "img2.jpg"), new(3, "Step 3", "img3.jpg") };
        var req = new CreateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 999, "Beginner", "Free", "cover.jpg", steps);
        var command = new AdminCreateTutorialCommand(Guid.NewGuid(), UserRoleType.Admin, req);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockTutorialRepo.Setup(r => r.GetActiveCategoryAsync(999, default)).ReturnsAsync((Category?)null);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("does not exist or is not active", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_SlugCollision_GeneratesUniqueSlug()
    {
        var steps = new List<CreateTutorialStepRequest> { new(1, "Step 1", "img.jpg"), new(2, "Step 2", "img2.jpg"), new(3, "Step 3", "img3.jpg") };
        var req = new CreateTutorialRequest("My Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", steps);
        var command = new AdminCreateTutorialCommand(Guid.NewGuid(), UserRoleType.Admin, req);

        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockTutorialRepo.Setup(r => r.GetActiveCategoryAsync(1, default)).ReturnsAsync(new Category());
        
        _mockTutorialRepo.SetupSequence(r => r.SlugExistsAsync(It.IsAny<string>(), default))
            .ReturnsAsync(true) // first time true
            .ReturnsAsync(false); // second time false

        var result = await _handler.HandleAsync(command);

        Assert.Equal("my-title-2", result.Slug);
    }

    [Fact]
    public async Task HandleAsync_DescriptionInvalidLength_ThrowsDomainException()
    {
        var req = new CreateTutorialRequest("Valid Title", "Short", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new AdminCreateTutorialCommand(Guid.NewGuid(), UserRoleType.Admin, req);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Description must be between 20 and 500 characters", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_DescriptionContainsBlockedWord_ThrowsDomainException()
    {
        var steps = new List<CreateTutorialStepRequest> { new(1, "Step 1", "img.jpg"), new(2, "Step 2", "img2.jpg"), new(3, "Step 3", "img3.jpg") };
        var req = new CreateTutorialRequest("Valid Title", "Bad description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", steps);
        var command = new AdminCreateTutorialCommand(Guid.NewGuid(), UserRoleType.Admin, req);
        
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Valid Title", default)).ReturnsAsync(false);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Bad description spanning 20 chars!", default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Description contains a blocked word", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_StepDescriptionContainsBlockedWord_ThrowsDomainException()
    {
        var steps = new List<CreateTutorialStepRequest> { new(1, "Bad Step", "img.jpg"), new(2, "Step 2", "img2.jpg"), new(3, "Step 3", "img3.jpg") };
        var req = new CreateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", steps);
        var command = new AdminCreateTutorialCommand(Guid.NewGuid(), UserRoleType.Admin, req);

        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Valid Title", default)).ReturnsAsync(false);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Valid description spanning 20 chars!", default)).ReturnsAsync(false);
        _mockTutorialRepo.Setup(r => r.GetActiveCategoryAsync(req.CategoryId, default)).ReturnsAsync(new Category());
        
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Bad Step", default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("description contains a blocked word", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_StepMissingDescriptionOrImage_ThrowsDomainException()
    {
        var steps = new List<CreateTutorialStepRequest> { new(1, "", "img.jpg"), new(2, "Step 2", "img2.jpg"), new(3, "Step 3", "img3.jpg") };
        var req = new CreateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", steps);
        var command = new AdminCreateTutorialCommand(Guid.NewGuid(), UserRoleType.Admin, req);

        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockTutorialRepo.Setup(r => r.GetActiveCategoryAsync(req.CategoryId, default)).ReturnsAsync(new Category());

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("needs both a description and an image to publish directly", ex.Message);
    }
}
