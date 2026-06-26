using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);

    /// <summary>
    /// Returns (rawToken, hashedToken, expiresAt).
    /// rawToken is sent to the client; hashedToken is stored in DB.
    /// </summary>
    (string RawToken, string HashedToken, DateTime ExpiresAt) GenerateRefreshToken();
}
