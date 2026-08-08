using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.Features.Tutorials.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Tutorials;

public class CreateTutorialHandlerTests
{
    private readonly Mock<ITutorialRepository> _mockTutorialRepo;
    private readonly Mock<IBlockedWordService> _mockBlockedWords;
    private readonly CreateTutorialHandler _handler;

    public CreateTutorialHandlerTests()
    {
        _mockTutorialRepo = new Mock<ITutorialRepository>();
        _mockBlockedWords = new Mock<IBlockedWordService>();
        _handler = new CreateTutorialHandler(_mockTutorialRepo.Object, _mockBlockedWords.Object);
    }

    [Fact]
    public async Task HandleAsync_InvalidTitleLength_ThrowsDomainException()
    {
        var req = new CreateTutorialRequest("A", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new CreateTutorialCommand(Guid.NewGuid(), req);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Title must be between 5 and 150 characters", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ContainsBlockedWord_ThrowsDomainException()
    {
        var req = new CreateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new CreateTutorialCommand(Guid.NewGuid(), req);

        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(req.Title, default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Title contains a blocked word", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_VipRequiresActiveSettings_ThrowsDomainException()
    {
        var req = new CreateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "VIP", "cover.jpg", null);
        var command = new CreateTutorialCommand(Guid.NewGuid(), req);

        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockTutorialRepo.Setup(r => r.GetActiveCreatorVipSettingsAsync(command.AuthorId, default)).ReturnsAsync((CreatorVipSettings?)null);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("configure a VIP pricing tier", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidCategory_ThrowsDomainException()
    {
        var req = new CreateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new CreateTutorialCommand(Guid.NewGuid(), req);

        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockTutorialRepo.Setup(r => r.GetActiveCategoryAsync(req.CategoryId, default)).ReturnsAsync((Category?)null);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("does not exist or is not active", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_CreatesTutorialWithUniqueSlug()
    {
        var steps = new List<CreateTutorialStepRequest> { new CreateTutorialStepRequest(1, "Step 1", null) };
        var req = new CreateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", steps);
        var command = new CreateTutorialCommand(Guid.NewGuid(), req);

        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockTutorialRepo.Setup(r => r.GetActiveCategoryAsync(req.CategoryId, default)).ReturnsAsync(new Category());
        
        _mockTutorialRepo.SetupSequence(r => r.SlugExistsAsync("valid-title", default))
            .ReturnsAsync(true)
            .ReturnsAsync(false); // second try 'valid-title-2' works

        var result = await _handler.HandleAsync(command);

        Assert.NotNull(result);
        Assert.Equal("valid-title-2", result.Slug);
        _mockTutorialRepo.Verify(r => r.AddAsync(It.Is<Tutorial>(t => t.Title == "Valid Title" && t.Steps.Count == 1), default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DescriptionInvalidLength_ThrowsDomainException()
    {
        var req = new CreateTutorialRequest("Valid Title", "Short", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new CreateTutorialCommand(Guid.NewGuid(), req);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Description must be between 20 and 500 characters", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_DescriptionContainsBlockedWord_ThrowsDomainException()
    {
        var req = new CreateTutorialRequest("Valid Title", "Bad description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new CreateTutorialCommand(Guid.NewGuid(), req);

        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Valid Title", default)).ReturnsAsync(false);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Bad description spanning 20 chars!", default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Description contains a blocked word", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_MetaTitleContainsBlockedWord_ThrowsDomainException()
    {
        var req = new CreateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null) { MetaTitle = "Bad Meta Title" };
        var command = new CreateTutorialCommand(Guid.NewGuid(), req);

        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Bad Meta Title", default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Meta title contains a blocked word", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_MetaDescriptionContainsBlockedWord_ThrowsDomainException()
    {
        var req = new CreateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null) { MetaDescription = "Bad Meta Desc" };
        var command = new CreateTutorialCommand(Guid.NewGuid(), req);

        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Bad Meta Desc", default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Meta description contains a blocked word", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidType_ThrowsDomainException()
    {
        var req = new CreateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "InvalidType", "cover.jpg", null);
        var command = new CreateTutorialCommand(Guid.NewGuid(), req);

        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Invalid tutorial type", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_InvalidDifficulty_ThrowsDomainException()
    {
        var req = new CreateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "InvalidDiff", "Free", "cover.jpg", null);
        var command = new CreateTutorialCommand(Guid.NewGuid(), req);

        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Invalid difficulty", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_StepDescriptionContainsBlockedWord_ThrowsDomainException()
    {
        var steps = new List<CreateTutorialStepRequest> { new(1, "Bad Step", "img.jpg") };
        var req = new CreateTutorialRequest("Valid Title", "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", steps);
        var command = new CreateTutorialCommand(Guid.NewGuid(), req);

        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockTutorialRepo.Setup(r => r.GetActiveCategoryAsync(req.CategoryId, default)).ReturnsAsync(new Category());
        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Bad Step", default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("description contains a blocked word", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_LongTitle_TruncatesSlug()
    {
        string longTitle = new string('A', 150); // 150 chars > 110 limit
        var req = new CreateTutorialRequest(longTitle, "Valid description spanning 20 chars!", 1, "Beginner", "Free", "cover.jpg", null);
        var command = new CreateTutorialCommand(Guid.NewGuid(), req);

        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _mockTutorialRepo.Setup(r => r.GetActiveCategoryAsync(req.CategoryId, default)).ReturnsAsync(new Category());
        _mockTutorialRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), default)).ReturnsAsync(false);

        var result = await _handler.HandleAsync(command);

        Assert.NotNull(result);
        Assert.Equal(110, result.Slug.Length);
        Assert.Equal(new string('a', 110), result.Slug);
    }
}
