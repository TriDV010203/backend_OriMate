using OrigamiPlatform.Application.DTOs.Gamification;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Queries.Gamification;

public class GetMySkillLevelHandler
{
    private readonly IUserRepository _users;

    public GetMySkillLevelHandler(IUserRepository users) => _users = users;

    public async Task<SkillLevelDto> HandleAsync(GetMySkillLevelQuery query, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(query.UserId, ct)
            ?? throw new NotFoundException("User not found.");

        if (user.Profile is null)
            throw new NotFoundException("User profile not found.");

        return new SkillLevelDto(user.Profile.SkillPoints, user.Profile.SkillLevel.ToString());
    }
}
