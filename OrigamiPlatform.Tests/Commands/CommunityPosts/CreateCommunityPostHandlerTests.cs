using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.CommunityPosts;
using OrigamiPlatform.Application.DTOs.CommunityPosts;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.CommunityPosts;

public class CreateCommunityPostHandlerTests
{
    private readonly Mock<ICommunityPostRepository> _mockPosts;
    private readonly Mock<IBlockedWordService> _mockBlockedWords;
    private readonly CreateCommunityPostHandler _handler;

    public CreateCommunityPostHandlerTests()
    {
        _mockPosts = new Mock<ICommunityPostRepository>();
        _mockBlockedWords = new Mock<IBlockedWordService>();
        _handler = new CreateCommunityPostHandler(_mockPosts.Object, _mockBlockedWords.Object);
    }

    [Fact]
    public async Task HandleAsync_EmptyContent_ThrowsDomainException()
    {
        var command = new CreateCommunityPostCommand(Guid.NewGuid(), "", null, null);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("between 1 and 1,000 characters", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ContentTooLong_ThrowsDomainException()
    {
        var longContent = new string('a', 1001);
        var command = new CreateCommunityPostCommand(Guid.NewGuid(), longContent, null, null);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("between 1 and 1,000 characters", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_TooManyMediaItems_ThrowsDomainException()
    {
        var mediaItems = Enumerable.Range(0, 11).Select(i => new MediaItemDto { MediaUrl = $"url{i}.jpg", MediaType = MediaType.Image }).ToList();
        var command = new CreateCommunityPostCommand(Guid.NewGuid(), "Valid content", null, mediaItems);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("maximum of 10 media items", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ContentContainsBlockedWord_ThrowsDomainException()
    {
        var command = new CreateCommunityPostCommand(Guid.NewGuid(), "Bad post", null, null);

        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync("Bad post", default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("blocked words", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidPostWithoutMedia_CreatesPost()
    {
        var command = new CreateCommunityPostCommand(Guid.NewGuid(), "Valid post content", Guid.NewGuid(), null);

        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockPosts.Verify(r => r.AddAsync(It.Is<CommunityPost>(p => 
            p.Content == "Valid post content" && 
            p.AuthorId == command.UserId &&
            p.LinkedTutorialId == command.TutorialId &&
            p.IsVisible && 
            !p.IsDeleted &&
            (p.Media == null || !p.Media.Any())
        )), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ValidPostWithMedia_CreatesPostWithMedia()
    {
        var mediaItems = new List<MediaItemDto> 
        { 
            new() { MediaUrl = "img1.jpg", MediaType = MediaType.Image },
            new() { MediaUrl = "vid1.mp4", MediaType = MediaType.Video }
        };
        var command = new CreateCommunityPostCommand(Guid.NewGuid(), "Valid post content", null, mediaItems);

        _mockBlockedWords.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _mockPosts.Verify(r => r.AddAsync(It.Is<CommunityPost>(p => 
            p.Content == "Valid post content" && 
            p.Media != null && p.Media.Count == 2 &&
            p.Media.First().Url == "img1.jpg" && p.Media.First().DisplayOrder == 0 &&
            p.Media.Last().Url == "vid1.mp4" && p.Media.Last().DisplayOrder == 1
        )), Times.Once);
    }
}
