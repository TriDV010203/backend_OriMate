using Moq;
using Xunit;
using OrigamiPlatform.Application.Commands.Comments;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Tests.Commands.Comments;

public class DeleteCommentHandlerTests
{
    private readonly Mock<ICommentRepository> _mockComments;
    private readonly DeleteCommentHandler _handler;

    public DeleteCommentHandlerTests()
    {
        _mockComments = new Mock<ICommentRepository>();
        _handler = new DeleteCommentHandler(_mockComments.Object);
    }

    [Fact]
    public async Task HandleAsync_CommentNotFound_ThrowsDomainException()
    {
        var command = new DeleteCommentCommand(Guid.NewGuid(), Guid.NewGuid());
        _mockComments.Setup(c => c.GetByIdAsync(command.CommentId)).ReturnsAsync((Comment?)null);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.HandleAsync(command));
        Assert.Contains("Comment not found", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_UserNotAuthor_ThrowsForbiddenException()
    {
        var command = new DeleteCommentCommand(Guid.NewGuid(), Guid.NewGuid());
        var comment = new Comment { Id = command.CommentId, AuthorId = Guid.NewGuid() };
        _mockComments.Setup(c => c.GetByIdAsync(command.CommentId)).ReturnsAsync(comment);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _handler.HandleAsync(command));
        Assert.Contains("not allowed to delete", ex.Message);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_SoftDeletesComment()
    {
        var command = new DeleteCommentCommand(Guid.NewGuid(), Guid.NewGuid());
        var comment = new Comment { Id = command.CommentId, AuthorId = command.UserId, IsDeleted = false };
        _mockComments.Setup(c => c.GetByIdAsync(command.CommentId)).ReturnsAsync(comment);

        await _handler.HandleAsync(command);

        Assert.True(comment.IsDeleted);
        _mockComments.Verify(c => c.UpdateAsync(comment), Times.Once);
    }
}
