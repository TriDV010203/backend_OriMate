using OrigamiPlatform.Application.DTOs.Achievements;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Achievements;

public class CreateAchievementHandler
{
    private const int MaxNoteLength = 500;
    private const int MaxPhotoUrlLength = 512;

    private readonly IAchievementRepository _achievements;

    public CreateAchievementHandler(IAchievementRepository achievements)
        => _achievements = achievements;

    public async Task<AchievementDto> HandleAsync(
        CreateAchievementCommand command,
        CancellationToken ct = default)
    {
        Validate(command.Request.PhotoUrl, command.Request.Note);

        var tutorialExists = await _achievements.PublishedTutorialExistsAsync(
            command.Request.TutorialId, ct);
        if (!tutorialExists)
            throw new NotFoundException("Published tutorial not found.");

        var existing = await _achievements.GetByUserAndTutorialAsync(
            command.UserId,
            command.Request.TutorialId,
            ct);
        if (existing is not null)
            throw new DomainException("You already marked this tutorial as completed.");

        var now = DateTime.UtcNow;
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            TutorialId = command.Request.TutorialId,
            PhotoUrl = command.Request.PhotoUrl,
            Note = command.Request.Note,
            IsPublic = command.Request.IsPublic,
            CreatedAt = now
        };

        await _achievements.AddAsync(achievement, ct);

        var created = await _achievements.GetByIdAsync(achievement.Id, ct)
            ?? throw new NotFoundException("Achievement not found after creation.");

        return created.ToDto();
    }

    private static void Validate(string? photoUrl, string? note)
    {
        if (photoUrl is { Length: > MaxPhotoUrlLength })
            throw new DomainException($"Photo URL must not exceed {MaxPhotoUrlLength} characters.");

        if (note is { Length: > MaxNoteLength })
            throw new DomainException($"Note must not exceed {MaxNoteLength} characters.");
    }
}
