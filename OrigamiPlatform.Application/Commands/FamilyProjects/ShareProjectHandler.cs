using OrigamiPlatform.Application.DTOs.FamilyProjects;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.FamilyProjects;

public class ShareProjectHandler
{
    private const string Label = "[Family Project]";
    private const int MaxNoteLength = 1800;

    private readonly IFamilyProjectRepository _projects;
    private readonly ICommunityPostRepository _posts;
    private readonly IBlockedWordService _blockedWords;

    public ShareProjectHandler(
        IFamilyProjectRepository projects,
        ICommunityPostRepository posts,
        IBlockedWordService blockedWords)
        => (_projects, _posts, _blockedWords) = (projects, posts, blockedWords);

    public async Task<SharedProjectPostDto> HandleAsync(
        ShareProjectCommand command,
        CancellationToken ct = default)
    {
        var project = await _projects.GetByIdWithMembersAsync(command.ProjectId, ct)
            ?? throw new NotFoundException("Family project not found.");

        // UC-40: only the owner may share, and only once the project is completed.
        if (project.OwnerId != command.UserId)
            throw new ForbiddenException("Only the project owner can share the project.");

        if (project.Status != ProjectStatus.Completed)
            throw new DomainException("Only a completed project can be shared to the community.");

        var note = (command.Note ?? string.Empty).Trim();
        if (note.Length > MaxNoteLength)
            throw new DomainException($"Summary note must not exceed {MaxNoteLength} characters.");

        // BR-23: screen the user-written note for blocked words.
        if (note.Length > 0 && await _blockedWords.ContainsBlockedWordAsync(note, ct))
            throw new DomainException("Your note contains blocked words and cannot be posted.");

        // AC-04: the post is labelled 'Family Project' and linked to the tutorial.
        var body = note.Length == 0
            ? $"We completed our family origami project: {project.Name}."
            : note;
        var content = $"{Label} {body}";

        var now = DateTime.UtcNow;
        var post = new CommunityPost
        {
            Id = Guid.NewGuid(),
            AuthorId = command.UserId,
            LinkedTutorialId = project.TutorialId,
            Content = content,
            IsVisible = true,
            IsDeleted = false,
            CreatedAt = now
        };

        await _posts.AddAsync(post);

        return new SharedProjectPostDto(post.Id, project.Id, content, now);
    }
}
