using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.DTOs.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrigamiPlatform.Application.UnitTests.Tutorials.Commands
{
    public class CreateWorkingCopyCommandHandlerTests
    {
        private readonly Mock<ITutorialRepository> _tutorialRepoMock;
        private readonly CreateWorkingCopyHandler _handler;

        public CreateWorkingCopyCommandHandlerTests()
        {
            _tutorialRepoMock = new Mock<ITutorialRepository>();
            _handler = new CreateWorkingCopyHandler(_tutorialRepoMock.Object);
        }

        [Fact]
        public async Task HandleAsync_TutorialNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var command = new CreateWorkingCopyCommand(Guid.NewGuid(), Guid.NewGuid());
            _tutorialRepoMock.Setup(x => x.GetByIdWithStepsAsync(command.TutorialId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tutorial)null);

            // Act
            Func<Task> act = async () => await _handler.HandleAsync(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Tutorial {command.TutorialId} not found.");
        }

        [Fact]
        public async Task HandleAsync_NotAuthor_ThrowsForbiddenException()
        {
            // Arrange
            var command = new CreateWorkingCopyCommand(Guid.NewGuid(), Guid.NewGuid());
            var originalTutorial = new Tutorial
            {
                Id = command.TutorialId,
                AuthorId = Guid.NewGuid() // Different author
            };

            _tutorialRepoMock.Setup(x => x.GetByIdWithStepsAsync(command.TutorialId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(originalTutorial);

            // Act
            Func<Task> act = async () => await _handler.HandleAsync(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>()
                .WithMessage("You are not the author of this tutorial.");
        }

        [Fact]
        public async Task HandleAsync_NotPublished_ThrowsDomainException()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var command = new CreateWorkingCopyCommand(Guid.NewGuid(), authorId);
            var originalTutorial = new Tutorial
            {
                Id = command.TutorialId,
                AuthorId = authorId,
                Status = TutorialStatus.Draft // Not published
            };

            _tutorialRepoMock.Setup(x => x.GetByIdWithStepsAsync(command.TutorialId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(originalTutorial);

            // Act
            Func<Task> act = async () => await _handler.HandleAsync(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("Only published tutorials can be edited.");
        }

        [Fact]
        public async Task HandleAsync_WorkingCopyAlreadyExists_ThrowsDomainException()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var command = new CreateWorkingCopyCommand(Guid.NewGuid(), authorId);
            var originalTutorial = new Tutorial
            {
                Id = command.TutorialId,
                AuthorId = authorId,
                Status = TutorialStatus.Published
            };
            var existingWorkingCopy = new Tutorial();

            _tutorialRepoMock.Setup(x => x.GetByIdWithStepsAsync(command.TutorialId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(originalTutorial);
            _tutorialRepoMock.Setup(x => x.GetWorkingCopyByParentIdAsync(command.TutorialId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingWorkingCopy);

            // Act
            Func<Task> act = async () => await _handler.HandleAsync(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("An edit is already in progress for this tutorial.");
        }

        [Fact]
        public async Task HandleAsync_ValidRequest_CreatesWorkingCopyAndReturnsResponse()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var command = new CreateWorkingCopyCommand(Guid.NewGuid(), authorId);
            var originalTutorial = new Tutorial
            {
                Id = command.TutorialId,
                AuthorId = authorId,
                Status = TutorialStatus.Published,
                Slug = "test-tutorial",
                Title = "Test Tutorial",
                Description = "Description",
                CoverImageUrl = "image.jpg",
                Type = TutorialType.Free,
                Difficulty = TutorialDifficulty.Beginner,
                Steps = new List<TutorialStep>
                {
                    new TutorialStep { Id = Guid.NewGuid(), StepOrder = 1, Description = "Step 1" }
                }
            };

            _tutorialRepoMock.Setup(x => x.GetByIdWithStepsAsync(command.TutorialId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(originalTutorial);
            _tutorialRepoMock.Setup(x => x.GetWorkingCopyByParentIdAsync(command.TutorialId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tutorial)null);
            _tutorialRepoMock.Setup(x => x.SlugExistsAsync("test-tutorial-edit", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            Tutorial capturedWorkingCopy = null;
            _tutorialRepoMock.Setup(x => x.AddAsync(It.IsAny<Tutorial>(), It.IsAny<CancellationToken>()))
                .Callback<Tutorial, CancellationToken>((t, _) => capturedWorkingCopy = t)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.HandleAsync(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.OriginalId.Should().Be(originalTutorial.Id);
            result.Status.Should().Be(TutorialStatus.EditPendingReview.ToString());

            capturedWorkingCopy.Should().NotBeNull();
            capturedWorkingCopy.AuthorId.Should().Be(authorId);
            capturedWorkingCopy.ParentTutorialId.Should().Be(originalTutorial.Id);
            capturedWorkingCopy.Title.Should().Be(originalTutorial.Title);
            capturedWorkingCopy.Slug.Should().Be("test-tutorial-edit");
            capturedWorkingCopy.Status.Should().Be(TutorialStatus.EditPendingReview);
            capturedWorkingCopy.Steps.Should().HaveCount(1);
            capturedWorkingCopy.Steps.First().Description.Should().Be("Step 1");
            
            _tutorialRepoMock.Verify(x => x.AddAsync(It.IsAny<Tutorial>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
