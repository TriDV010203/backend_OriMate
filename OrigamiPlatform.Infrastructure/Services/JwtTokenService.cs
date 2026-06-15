using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Services;

public class JwtTokenService : ITokenService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;
    private readonly int _refreshTokenExpiryDays;

    public JwtTokenService(IConfiguration config)
    {
        _key = config["Jwt:Key"]!;
        _issuer = config["Jwt:Issuer"]!;
        _audience = config["Jwt:Audience"]!;
        _expiryMinutes = int.Parse(config["Jwt:ExpiryMinutes"]!);
        _refreshTokenExpiryDays = int.Parse(config["Jwt:RefreshTokenExpiryDays"] ?? "30");
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in user.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role.Role.ToString()));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public (string RawToken, string HashedToken, DateTime ExpiresAt) GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var rawToken = Convert.ToBase64String(bytes);
        var hashedToken = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        var expiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);
        return (rawToken, hashedToken, expiresAt);
    }
}
