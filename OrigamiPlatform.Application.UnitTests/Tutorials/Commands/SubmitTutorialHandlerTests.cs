using FluentAssertions;
using Moq;
using OrigamiPlatform.Application.Commands.Tutorials;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;
using Xunit;

namespace OrigamiPlatform.Application.UnitTests.Tutorials.Commands;

/// <summary>
/// Unit tests for <see cref="SubmitTutorialHandler"/> (FT-09 — Creator submits a draft for manager review).
/// Pattern : Arrange – Act – Assert.
/// Naming  : [MethodName]_[Scenario]_[ExpectedResult]
///
/// Key business rules under test:
///   BR-12  : Title 5–150 chars, Description 20–500 chars, CoverImage required,
///             3–30 steps each with non-empty Description and ImageUrl.
///   BR-TUT-01 : Draft/RevisionRequired → PendingManagerReview (single review round).
/// </summary>
public class SubmitTutorialHandlerTests
{
    // ──────────────────────────────────────────────
    // Shared mocks
    // ──────────────────────────────────────────────
    private readonly Mock<ITutorialRepository>   _tutorialRepoMock   = new();
    private readonly Mock<INotificationService>  _notificationsMock  = new();

    private SubmitTutorialHandler CreateSut() =>
        new(_tutorialRepoMock.Object, _notificationsMock.Object);

    // ──────────────────────────────────────────────
    // Helpers – minimal entity builders
    // ──────────────────────────────────────────────

    /// <summary>
    /// Creates a valid <see cref="Tutorial"/> in Draft status owned by <paramref name="authorId"/>
    /// with exactly <paramref name="stepCount"/> fully-formed steps and a cover image.
    /// </summary>
    private static Tutorial BuildDraftTutorial(
        Guid   authorId,
        int    stepCount     = 3,
        string? coverImageUrl = "https://cdn.example.com/cover.jpg") =>
        new()
        {
            Id           = Guid.NewGuid(),
            AuthorId     = authorId,
            CategoryId   = 1,
            Title        = "How to Fold a Paper Crane",
            Description  = "A beginner-friendly step-by-step origami tutorial for the classic crane.",
            Slug         = "how-to-fold-a-paper-crane",
            CoverImageUrl = coverImageUrl,
            Type         = TutorialType.Free,
            Difficulty   = TutorialDifficulty.Beginner,
            Status       = TutorialStatus.Draft,
            CreatedAt    = DateTime.UtcNow,
            Steps        = Enumerable.Range(1, stepCount)
                .Select(i => new TutorialStep
                {
                    Id          = Guid.NewGuid(),
                    StepOrder   = i,
                    Description = $"Step {i} description.",
                    ImageUrl    = $"https://cdn.example.com/step{i}.jpg"
                })
                .ToList<TutorialStep>()
        };

    /// <summary>
    /// Sets up all shared mocks required for a successful Submit flow:
    /// <list type="bullet">
    ///   <item><description>Repository returns the given tutorial.</description></item>
    ///   <item><description>Category lookup returns an active category.</description></item>
    ///   <item><description>UpdateAsync and AddReviewHistoryAsync succeed.</description></item>
    ///   <item><description>Notification dispatch succeeds.</description></item>
    /// </list>
    /// </summary>
    private void SetupRepositoryForHappyPath(Tutorial tutorial)
    {
        _tutorialRepoMock
            .Setup(r => r.GetByIdWithStepsAsync(tutorial.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tutorial);

        _tutorialRepoMock
            .Setup(r => r.GetActiveCategoryAsync(tutorial.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { Id = tutorial.CategoryId, Name = "Origami Cơ Bản", IsActive = true });

        _tutorialRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Tutorial>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tutorialRepoMock
            .Setup(r => r.AddReviewHistoryAsync(It.IsAny<TutorialReviewHistory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _notificationsMock
            .Setup(n => n.NotifyUsersWithRoleAsync(
                It.IsAny<UserRoleType>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // TC-01 : HandleAsync_ValidDraft_UpdatesStatusToPending
    //         Happy path — draft has ≥ 3 steps, a cover image, valid title &
    //         description, and an active category.
    //         Expected: status transitions Draft → PendingManagerReview,
    //         review history is inserted, and managers are notified.
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_ValidDraft_UpdatesStatusToPending()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var authorId = Guid.NewGuid();
        var tutorial = BuildDraftTutorial(authorId, stepCount: 3);
        var command  = new SubmitTutorialCommand(tutorial.Id, authorId);

        SetupRepositoryForHappyPath(tutorial);

        Tutorial? updatedTutorial = null;
        _tutorialRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Tutorial>(), It.IsAny<CancellationToken>()))
            .Callback<Tutorial, CancellationToken>((t, _) => updatedTutorial = t)
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────────────
        var result = await sut.HandleAsync(command);

        // ── Assert ────────────────────────────────────────────────────────────

        // State transition
        result.Should().NotBeNull();
        result.Status.Should().Be(TutorialStatus.PendingManagerReview.ToString(),
            "a valid draft must transition to PendingManagerReview after Submit (BR-TUT-01)");
        result.Id.Should().Be(tutorial.Id);

        // The entity persisted via UpdateAsync reflects the new status
        updatedTutorial.Should().NotBeNull();
        updatedTutorial!.Status.Should().Be(TutorialStatus.PendingManagerReview);
        updatedTutorial.UpdatedAt.Should().NotBeNull();

        // Review history MUST be appended (BR-17 — immutable insert-only log)
        _tutorialRepoMock.Verify(
            r => r.AddReviewHistoryAsync(
                It.Is<TutorialReviewHistory>(h =>
                    h.TutorialId   == tutorial.Id                         &&
                    h.FromStatus   == TutorialStatus.Draft                &&
                    h.ToStatus     == TutorialStatus.PendingManagerReview &&
                    h.Action       == "Submit"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Managers must be notified
        _notificationsMock.Verify(
            n => n.NotifyUsersWithRoleAsync(
                UserRoleType.Manager,
                NotificationType.TutorialReadyForManagerApproval,
                It.IsAny<string>(),
                "Tutorial",
                tutorial.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // TC-02 : HandleAsync_LessThanThreeSteps_ThrowsDomainException
    //         Business Rule (BR-12): A tutorial must have at least 3 steps to be
    //         submitted. Having only 2 steps must cause a DomainException, and the
    //         repository UpdateAsync must never be called.
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_LessThanThreeSteps_ThrowsDomainException()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var authorId = Guid.NewGuid();
        var tutorial = BuildDraftTutorial(authorId, stepCount: 2); // only 2 steps — violates BR-12
        var command  = new SubmitTutorialCommand(tutorial.Id, authorId);

        // Repository returns the under-stepped tutorial; category is valid
        _tutorialRepoMock
            .Setup(r => r.GetByIdWithStepsAsync(tutorial.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tutorial);

        _tutorialRepoMock
            .Setup(r => r.GetActiveCategoryAsync(tutorial.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { Id = tutorial.CategoryId, Name = "Origami Cơ Bản", IsActive = true });

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────────────
        var act = () => sut.HandleAsync(command);

        // ── Assert ────────────────────────────────────────────────────────────
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*must have between 3 and 30 steps*");

        // The tutorial must NOT be updated in the database
        _tutorialRepoMock.Verify(
            r => r.UpdateAsync(It.IsAny<Tutorial>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // No review history should be logged
        _tutorialRepoMock.Verify(
            r => r.AddReviewHistoryAsync(It.IsAny<TutorialReviewHistory>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // No notifications should be dispatched
        _notificationsMock.Verify(
            n => n.NotifyUsersWithRoleAsync(
                It.IsAny<UserRoleType>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // TC-03 : HandleAsync_TutorialNotOwnedByCurrentUser_ThrowsForbiddenException
    //         Security: A creator must only be able to submit their OWN drafts.
    //         When AuthorId on the command does not match the tutorial's AuthorId,
    //         the handler must throw ForbiddenException immediately — before any
    //         business-rule validation or database mutations.
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_TutorialNotOwnedByCurrentUser_ThrowsForbiddenException()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var realAuthorId    = Guid.NewGuid();  // the legitimate owner
        var intruderAuthorId = Guid.NewGuid(); // an attacker trying to submit someone else's tutorial

        var tutorial = BuildDraftTutorial(realAuthorId, stepCount: 3);

        // Command is issued by the intruder, not the real author
        var command = new SubmitTutorialCommand(tutorial.Id, intruderAuthorId);

        _tutorialRepoMock
            .Setup(r => r.GetByIdWithStepsAsync(tutorial.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tutorial);

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────────────
        var act = () => sut.HandleAsync(command);

        // ── Assert ────────────────────────────────────────────────────────────
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*not the author*");

        // Authorship check must short-circuit — no DB mutations, no notifications
        _tutorialRepoMock.Verify(
            r => r.UpdateAsync(It.IsAny<Tutorial>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _tutorialRepoMock.Verify(
            r => r.AddReviewHistoryAsync(It.IsAny<TutorialReviewHistory>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _notificationsMock.Verify(
            n => n.NotifyUsersWithRoleAsync(
                It.IsAny<UserRoleType>(),
                It.IsAny<NotificationType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // TC-04 : HandleAsync_MissingCoverImage_ThrowsDomainException
    //         BR-12: CoverImageUrl is REQUIRED at submission time.
    //         A draft without a cover image must fail with DomainException,
    //         regardless of how many valid steps it has.
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_MissingCoverImage_ThrowsDomainException()
    {
        // ── Arrange ───────────────────────────────────────────────────────────
        var authorId = Guid.NewGuid();
        var tutorial = BuildDraftTutorial(authorId, stepCount: 3, coverImageUrl: null); // no cover
        var command  = new SubmitTutorialCommand(tutorial.Id, authorId);

        _tutorialRepoMock
            .Setup(r => r.GetByIdWithStepsAsync(tutorial.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tutorial);

        _tutorialRepoMock
            .Setup(r => r.GetActiveCategoryAsync(tutorial.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { Id = tutorial.CategoryId, Name = "Origami Cơ Bản", IsActive = true });

        var sut = CreateSut();

        // ── Act ───────────────────────────────────────────────────────────────
        var act = () => sut.HandleAsync(command);

        // ── Assert ────────────────────────────────────────────────────────────
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Cover image is required*");

        _tutorialRepoMock.Verify(
            r => r.UpdateAsync(It.IsAny<Tutorial>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
