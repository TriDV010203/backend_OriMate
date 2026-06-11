using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Achievements;

public class DeleteAchievementHandler
{
    private readonly IAchievementRepository _achievements;

    public DeleteAchievementHandler(IAchievementRepository achievements)
        => _achievements = achievements;

    public async Task HandleAsync(DeleteAchievementCommand command, CancellationToken ct = default)
    {
        var achievement = await _achievements.GetByIdAsync(command.AchievementId, ct)
            ?? throw new NotFoundException("Achievement not found.");

        if (achievement.UserId != command.UserId)
            throw new ForbiddenException("You can only delete your own achievement.");

        await _achievements.DeleteAsync(achievement, ct);
    }
}
