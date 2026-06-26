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
    private readonly IEmailService _email;

    public RegisterUserHandler(
        IUserRepository users,
        IPasswordHasher hasher,
        ITokenService tokens,
        IEmailService email)
        => (_users, _hasher, _tokens, _email) = (users, hasher, tokens, email);

    public async Task<AuthResponse> HandleAsync(RegisterUserCommand cmd, CancellationToken ct = default)
    {
        if (await _users.ExistsByEmailAsync(cmd.Email.ToLowerInvariant(), ct))
            throw new DomainException("Email is already registered.");

        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var verificationToken = Guid.NewGuid().ToString("N");

        var user = new User
        {
            Id = userId,
            Email = cmd.Email.ToLowerInvariant(),
            PasswordHash = _hasher.Hash(cmd.Password),
            Status = AccountStatus.Unverified,
            VerificationToken = verificationToken,
            TokenExpiry = now.AddHours(24),
            CreatedAt = now,
            Profile = new UserProfile
            {
                UserId = userId,
                DisplayName = cmd.DisplayName,
                CreatedAt = now
            },
            Roles = new List<UserRole>
            {
                new UserRole { UserId = userId, Role = UserRoleType.User, CreatedAt = now }
            }
        };

        var (rawRefresh, hashedRefresh, refreshExpiresAt) = _tokens.GenerateRefreshToken();
        user.RefreshTokenHash = hashedRefresh;
        user.RefreshTokenExpiresAt = refreshExpiresAt;

        await _users.AddAsync(user, ct);
        await _email.SendVerificationEmailAsync(user.Email, verificationToken, ct);

        var (token, expiresAt) = _tokens.GenerateToken(user);
        var roles = user.Roles.Select(r => r.Role.ToString()).ToList();

        return new AuthResponse(user.Id, user.Email, cmd.DisplayName, roles, token, expiresAt, rawRefresh);
    }
}
