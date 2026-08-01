using OrigamiPlatform.Application.DTOs.Users;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.Users;

public class GetOnboardingStatusHandler
{
    private readonly IUserRepository _users;

    public GetOnboardingStatusHandler(IUserRepository users) => _users = users;

    public async Task<OnboardingStatusDto> HandleAsync(GetOnboardingStatusQuery query, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(query.UserId, ct)
            ?? throw new NotFoundException("User not found.");

        return new OnboardingStatusDto(user.Profile?.IsOnboardingCompleted ?? false);
    }
}
