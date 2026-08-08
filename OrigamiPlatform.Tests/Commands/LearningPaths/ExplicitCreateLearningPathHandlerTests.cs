using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.LearningPaths;
using OrigamiPlatform.Application.DTOs.LearningPaths;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.LearningPaths;

public class ExplicitCreateLearningPathHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidData_CreatesLearningPath()
    {
        var lpRepo = new Mock<ILearningPathRepository>();
        var tutRepo = new Mock<ITutorialRepository>();
        var blocked = new Mock<IBlockedWordService>();

        var handler = new CreateLearningPathHandler(lpRepo.Object, tutRepo.Object, blocked.Object);
        var tutId = Guid.NewGuid();
        
        var request = new CreateLearningPathRequest(
            Title: "Official Learning Path",
            Description: "This is a very long and detailed description for a learning path.",
            CoverImageUrl: "http://test.com",
            TutorialIds: new List<Guid> { tutId }
        );

        blocked.Setup(x => x.ContainsBlockedWordAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        
        var tutorial = new Tutorial 
        { 
            Id = tutId, 
            IsOfficial = true, 
            Status = TutorialStatus.Published, 
            IsDeleted = false, Category = new Category { Name = "Test" } 
        };
        
        tutRepo.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), default)).ReturnsAsync(new List<Tutorial> { tutorial });

        var cmd = new CreateLearningPathCommand(Guid.NewGuid(), request);
        var result = await handler.HandleAsync(cmd);

        Assert.NotNull(result);
        Assert.Equal(LearningPathStatus.Draft.ToString(), result.Status);
        lpRepo.Verify(x => x.AddAsync(It.IsAny<LearningPath>(), default), Times.Once);
    }
}
