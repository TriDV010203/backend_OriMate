using System.Security.Cryptography;
using System.Text;
using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.Auth;

public class RefreshTokenHandler
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;

    public RefreshTokenHandler(IUserRepository users, ITokenService tokens)
        => (_users, _tokens) = (users, tokens);

    public async Task<AuthResponse> HandleAsync(RefreshTokenCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.RefreshToken))
            throw new DomainException("Refresh token is required.");

        var hashedIncoming = HashToken(cmd.RefreshToken);

        var user = await _users.GetByRefreshTokenHashAsync(hashedIncoming, ct)
            ?? throw new DomainException("Invalid or expired refresh token.");

        if (user.RefreshTokenExpiresAt < DateTime.UtcNow)
            throw new DomainException("Refresh token has expired. Please log in again.");

        var (accessToken, expiresAt) = _tokens.GenerateToken(user);
        var (rawRefresh, hashedRefresh, refreshExpiresAt) = _tokens.GenerateRefreshToken();

        user.RefreshTokenHash = hashedRefresh;
        user.RefreshTokenExpiresAt = refreshExpiresAt;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);

        var roles = user.Roles.Select(r => r.Role.ToString()).ToList();

        return new AuthResponse(user.Id, user.Email, user.Profile?.DisplayName, roles, accessToken, expiresAt, rawRefresh);
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }
}
