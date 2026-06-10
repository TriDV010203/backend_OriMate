using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Auth;

public class RegisterUserHandler
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public RegisterUserHandler(IUserRepository users, IPasswordHasher hasher, ITokenService tokens)
        => (_users, _hasher, _tokens) = (users, hasher, tokens);

    public async Task<AuthResponse> HandleAsync(RegisterUserCommand cmd, CancellationToken ct = default)
    {
        if (await _users.ExistsByEmailAsync(cmd.Email.ToLowerInvariant(), ct))
            throw new DomainException("Email is already registered.");

        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var user = new User
        {
            Id = userId,
            Email = cmd.Email.ToLowerInvariant(),
            PasswordHash = _hasher.Hash(cmd.Password),
            Status = AccountStatus.Active,
            CreatedAt = now,
            Profile = new UserProfile
            {
                UserId = userId,
                DisplayName = cmd.Email.Split('@')[0],
                CreatedAt = now
            },
            Roles = new List<UserRole>
            {
                new UserRole { UserId = userId, Role = UserRoleType.User, CreatedAt = now }
            }
        };

        await _users.AddAsync(user, ct);

        var (token, expiresAt) = _tokens.GenerateToken(user);
        var roles = user.Roles.Select(r => r.Role.ToString()).ToList();

        return new AuthResponse(user.Id, user.Email, roles, token, expiresAt);
    }
}
