using Moq;
using OrigamiPlatform.Application.Commands.Journals;
using OrigamiPlatform.Application.DTOs.Journals;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using Xunit;

namespace OrigamiPlatform.Tests.Commands.Journals;

public class ExplicitUpdateJournalHandlerTests
{
    private readonly Mock<IJournalRepository> _mockJournals;
    private readonly Mock<IBlockedWordService> _mockBlockedWords;
    private readonly UpdateJournalHandler _handler;

    public ExplicitUpdateJournalHandlerTests()
    {
        _mockJournals = new Mock<IJournalRepository>();
        _mockBlockedWords = new Mock<IBlockedWordService>();
        _handler = new UpdateJournalHandler(_mockJournals.Object, _mockBlockedWords.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_UpdatesAndReturnsJournal()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var journalId = Guid.NewGuid();
        var linkedTutorialId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "TestUser" };

        var existingJournal = new Journal
        {
            Id = journalId,
            UserId = userId,
            LinkedTutorialId = null,
            Content = "Old Content",
            ImageUrls = "[]",
            IsPublic = false,
            User = user
        };

        var request = new UpdateJournalRequest(
            LinkedTutorialId: linkedTutorialId,
            Content: "Updated Content",
            ImageUrls: new List<string> { "https://example.com/img.png" },
            IsPublic: true
        );

        var command = new UpdateJournalCommand(userId, journalId, request);

        _mockBlockedWords
            .Setup(x => x.ContainsBlockedWordAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockJournals
            .Setup(x => x.GetByIdAsync(journalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingJournal);

        _mockJournals
            .Setup(x => x.PublishedTutorialExistsAsync(linkedTutorialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockJournals
            .Setup(x => x.UpdateAsync(It.IsAny<Journal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(journalId, result.Id);
        Assert.Equal("Updated Content", result.Content);
        Assert.True(result.IsPublic);
        Assert.Equal(linkedTutorialId, result.LinkedTutorialId);

        _mockJournals.Verify(x => x.UpdateAsync(It.Is<Journal>(j => 
            j.Id == journalId && 
            j.Content == "Updated Content" &&
            j.LinkedTutorialId == linkedTutorialId &&
            j.IsPublic == true
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
