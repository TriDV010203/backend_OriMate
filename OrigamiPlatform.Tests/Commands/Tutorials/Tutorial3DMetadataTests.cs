using Moq;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.Features.Tutorials.DTOs;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Application.Queries.Tutorials;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Tests.Commands.Tutorials;

public class Tutorial3DMetadataTests
{
    private const string ModelUrl = "https://cdn.example.com/tutorial-models/crane.glb";
    private const string PosterUrl = "https://cdn.example.com/tutorial-models/crane.jpg";

    [Fact]
    public async Task CreateTutorial_With3DMetadata_PersistsAndReturnsUrls()
    {
        var tutorialRepo = new Mock<ITutorialRepository>();
        var blockedWords = new Mock<IBlockedWordService>();
        Tutorial? savedTutorial = null;

        blockedWords
            .Setup(s => s.ContainsBlockedWordAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        tutorialRepo
            .Setup(r => r.GetActiveCategoryAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { Id = 1, Name = "Animals", IsActive = true });
        tutorialRepo
            .Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        tutorialRepo
            .Setup(r => r.AddAsync(It.IsAny<Tutorial>(), It.IsAny<CancellationToken>()))
            .Callback<Tutorial, CancellationToken>((tutorial, _) => savedTutorial = tutorial)
            .Returns(Task.CompletedTask);

        var request = new CreateTutorialRequest(
            "Origami crane",
            "Fold a traditional origami crane step by step.",
            1,
            "Beginner",
            "Free",
            "https://cdn.example.com/crane-cover.jpg",
            Steps: null,
            Model3DUrl: ModelUrl,
            Model3DPosterUrl: PosterUrl);

        var response = await new CreateTutorialHandler(tutorialRepo.Object, blockedWords.Object)
            .HandleAsync(new CreateTutorialCommand(Guid.NewGuid(), request));

        Assert.NotNull(savedTutorial);
        Assert.Equal(ModelUrl, savedTutorial.Model3DUrl);
        Assert.Equal(PosterUrl, savedTutorial.Model3DPosterUrl);
        Assert.Equal(ModelUrl, response.Model3DUrl);
        Assert.Equal(PosterUrl, response.Model3DPosterUrl);
    }

    [Fact]
    public async Task CreateWorkingCopy_Copies3DMetadataFromPublishedTutorial()
    {
        var tutorialRepo = new Mock<ITutorialRepository>();
        var authorId = Guid.NewGuid();
        var original = CreateTutorial(authorId, TutorialStatus.Published);
        Tutorial? savedWorkingCopy = null;

        tutorialRepo
            .Setup(r => r.GetByIdWithStepsAsync(original.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(original);
        tutorialRepo
            .Setup(r => r.GetWorkingCopyByParentIdAsync(original.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tutorial?)null);
        tutorialRepo
            .Setup(r => r.SlugExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        tutorialRepo
            .Setup(r => r.AddAsync(It.IsAny<Tutorial>(), It.IsAny<CancellationToken>()))
            .Callback<Tutorial, CancellationToken>((tutorial, _) => savedWorkingCopy = tutorial)
            .Returns(Task.CompletedTask);

        await new CreateWorkingCopyHandler(tutorialRepo.Object)
            .HandleAsync(new CreateWorkingCopyCommand(original.Id, authorId));

        Assert.NotNull(savedWorkingCopy);
        Assert.Equal(ModelUrl, savedWorkingCopy.Model3DUrl);
        Assert.Equal(PosterUrl, savedWorkingCopy.Model3DPosterUrl);
    }

    [Fact]
    public async Task ApproveWorkingCopy_CopiesUpdated3DMetadataToOriginal()
    {
        var tutorialRepo = new Mock<ITutorialRepository>();
        var notifications = new Mock<INotificationService>();
        var authorId = Guid.NewGuid();
        var original = CreateTutorial(authorId, TutorialStatus.Published);
        original.Model3DUrl = "https://cdn.example.com/old.glb";
        original.Model3DPosterUrl = "https://cdn.example.com/old.jpg";

        var workingCopy = CreateTutorial(authorId, TutorialStatus.PendingManagerReview);
        workingCopy.ParentTutorialId = original.Id;

        tutorialRepo
            .Setup(r => r.GetByIdWithStepsAsync(workingCopy.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workingCopy);
        tutorialRepo
            .Setup(r => r.GetByIdWithStepsAsync(original.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(original);
        tutorialRepo
            .Setup(r => r.UpdateAsync(It.IsAny<Tutorial>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        tutorialRepo
            .Setup(r => r.DeleteStepsByTutorialIdAsync(original.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        tutorialRepo
            .Setup(r => r.AddStepsAsync(It.IsAny<IEnumerable<TutorialStep>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        tutorialRepo
            .Setup(r => r.AddReviewHistoryAsync(It.IsAny<TutorialReviewHistory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifications
            .Setup(n => n.NotifyUserAsync(
                authorId,
                NotificationType.TutorialEditPublished,
                It.IsAny<string>(),
                "Tutorial",
                original.Id,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await new ManagerApproveEditHandler(tutorialRepo.Object, notifications.Object)
            .HandleAsync(new ManagerApproveEditCommand(workingCopy.Id, Guid.NewGuid()));

        Assert.Equal(ModelUrl, original.Model3DUrl);
        Assert.Equal(PosterUrl, original.Model3DPosterUrl);
        Assert.Equal(TutorialStatus.Merged, workingCopy.Status);
    }

    [Fact]
    public async Task GetPublishedTutorial_Returns3DMetadataForFrontendViewer()
    {
        var tutorialRepo = new Mock<ITutorialRepository>();
        var vipSubscriptions = new Mock<IVipSubscriptionRepository>();
        var likes = new Mock<ILikeRepository>();
        var wishlists = new Mock<IWishlistRepository>();
        var tutorial = CreateTutorial(Guid.NewGuid(), TutorialStatus.Published);
        tutorial.Category = new Category { Id = 1, Name = "Animals", IsActive = true };
        tutorial.Author = new User { Id = tutorial.AuthorId, Email = "creator@example.com" };

        tutorialRepo
            .Setup(r => r.GetPublishedBySlugAsync(tutorial.Slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tutorial);
        likes
            .Setup(r => r.GetLikeCountAsync(tutorial.Id, TargetType.Tutorial))
            .ReturnsAsync(0);
        wishlists
            .Setup(r => r.GetWishlistCountAsync(tutorial.Id, TargetType.Tutorial, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var response = await new GetTutorialBySlugHandler(
                tutorialRepo.Object,
                vipSubscriptions.Object,
                likes.Object,
                wishlists.Object)
            .HandleAsync(new GetTutorialBySlugQuery(tutorial.Slug));

        Assert.Equal(ModelUrl, response.Model3DUrl);
        Assert.Equal(PosterUrl, response.Model3DPosterUrl);
    }

    private static Tutorial CreateTutorial(Guid authorId, TutorialStatus status) => new()
    {
        Id = Guid.NewGuid(),
        AuthorId = authorId,
        CategoryId = 1,
        Title = "Origami crane",
        Description = "Fold a traditional origami crane step by step.",
        Slug = $"origami-crane-{Guid.NewGuid():N}",
        CoverImageUrl = "https://cdn.example.com/crane-cover.jpg",
        Model3DUrl = ModelUrl,
        Model3DPosterUrl = PosterUrl,
        Type = TutorialType.Free,
        Difficulty = TutorialDifficulty.Beginner,
        Status = status,
        PublishedAt = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow
    };
}
