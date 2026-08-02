using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Users;

public class CompleteOnboardingHandler
{
    private readonly IUserRepository _users;

    public CompleteOnboardingHandler(IUserRepository users) => _users = users;

    public async Task HandleAsync(CompleteOnboardingCommand command, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("User not found.");

        if (user.Profile is null)
        {
            user.Profile = new UserProfile
            {
                UserId = user.Id,
                IsOnboardingCompleted = true,
                CreatedAt = DateTime.UtcNow
            };
        }
        else
        {
            user.Profile.IsOnboardingCompleted = true;
            user.Profile.UpdatedAt = DateTime.UtcNow;
        }

        await _users.UpdateAsync(user, ct);
    }
}
