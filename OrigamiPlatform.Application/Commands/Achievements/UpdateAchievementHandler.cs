using OrigamiPlatform.Application.DTOs.Achievements;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Achievements;

public class UpdateAchievementHandler
{
    private const int MaxNoteLength = 500;
    private const int MaxPhotoUrlLength = 512;

    private readonly IAchievementRepository _achievements;

    public UpdateAchievementHandler(IAchievementRepository achievements)
        => _achievements = achievements;

    public async Task<AchievementDto> HandleAsync(
        UpdateAchievementCommand command,
        CancellationToken ct = default)
    {
        Validate(command.Request.PhotoUrl, command.Request.Note);

        var achievement = await _achievements.GetByIdAsync(command.AchievementId, ct)
            ?? throw new NotFoundException("Achievement not found.");

        if (achievement.UserId != command.UserId)
            throw new ForbiddenException("You can only update your own achievement.");

        achievement.PhotoUrl = command.Request.PhotoUrl;
        achievement.Note = command.Request.Note;
        achievement.IsPublic = command.Request.IsPublic;
        achievement.UpdatedAt = DateTime.UtcNow;

        await _achievements.UpdateAsync(achievement, ct);

        return achievement.ToDto();
    }

    private static void Validate(string? photoUrl, string? note)
    {
        if (photoUrl is { Length: > MaxPhotoUrlLength })
            throw new DomainException($"Photo URL must not exceed {MaxPhotoUrlLength} characters.");

        if (note is { Length: > MaxNoteLength })
            throw new DomainException($"Note must not exceed {MaxNoteLength} characters.");
    }
}
