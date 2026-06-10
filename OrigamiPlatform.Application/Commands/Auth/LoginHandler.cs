using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Auth;

public class LoginHandler
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public LoginHandler(IUserRepository users, IPasswordHasher hasher, ITokenService tokens)
        => (_users, _hasher, _tokens) = (users, hasher, tokens);

    public async Task<AuthResponse> HandleAsync(LoginCommand cmd, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(cmd.Email.ToLowerInvariant(), ct);

        if (user is null || !_hasher.Verify(cmd.Password, user.PasswordHash))
            throw new DomainException("Invalid email or password.");

        if (user.Status == AccountStatus.Suspended)
            throw new ForbiddenException("Account is suspended.");

        var (token, expiresAt) = _tokens.GenerateToken(user);
        var roles = user.Roles.Select(r => r.Role.ToString()).ToList();

        return new AuthResponse(user.Id, user.Email, roles, token, expiresAt);
    }
}
