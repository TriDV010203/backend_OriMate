using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.Commands.LearningPaths;
using OrigamiPlatform.Application.DTOs.LearningPaths;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrigamiPlatform.Tests.Commands.LearningPaths;

public class ExplicitUpdateLearningPathHandlerTests
{
    private readonly Mock<ILearningPathRepository> _learningPathRepoMock;
    private readonly Mock<ITutorialRepository> _tutorialRepoMock;
    private readonly Mock<IBlockedWordService> _blockedWordsMock;
    private readonly UpdateLearningPathHandler _handler;

    public ExplicitUpdateLearningPathHandlerTests()
    {
        _learningPathRepoMock = new Mock<ILearningPathRepository>();
        _tutorialRepoMock = new Mock<ITutorialRepository>();
        _blockedWordsMock = new Mock<IBlockedWordService>();
        _handler = new UpdateLearningPathHandler(
            _learningPathRepoMock.Object, 
            _tutorialRepoMock.Object, 
            _blockedWordsMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_UpdatesLearningPathAndReturnsDto()
    {
        // Arrange
        var learningPathId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tutorialId = Guid.NewGuid();
        
        var request = new UpdateLearningPathRequest(
            "Valid Title", 
            "Valid description that is longer than 20 chars.", 
            "http://example.com/cover.jpg", 
            new List<Guid> { tutorialId }
        );
        
        var command = new UpdateLearningPathCommand(learningPathId, actorId, request);
        
        var learningPath = new LearningPath { Id = learningPathId, Title = "Old Title" };
        var tutorial = new Tutorial { Id = tutorialId, Title = "Tut 1", IsOfficial = true, Status = TutorialStatus.Published, IsDeleted = false };
        
        _learningPathRepoMock.Setup(r => r.GetByIdForAdminAsync(learningPathId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(learningPath);
            
        _blockedWordsMock.Setup(b => b.ContainsBlockedWordAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
            
        _tutorialRepoMock.Setup(r => r.GetByIdsAsync(request.TutorialIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tutorial> { tutorial });

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        learningPath.Title.Should().Be("Valid Title");
        learningPath.Description.Should().Be("Valid description that is longer than 20 chars.");
        learningPath.CoverImageUrl.Should().Be("http://example.com/cover.jpg");
        
        _learningPathRepoMock.Verify(r => r.UpdateAsync(learningPath, It.IsAny<CancellationToken>()), Times.Once);
        _learningPathRepoMock.Verify(r => r.ReplaceItemsAsync(learningPathId, It.Is<List<LearningPathItem>>(items => items.Count == 1 && items[0].TutorialId == tutorialId), It.IsAny<CancellationToken>()), Times.Once);
        
        result.Should().NotBeNull();
        result.Id.Should().Be(learningPathId);
    }
}
