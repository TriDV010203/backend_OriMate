using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace OrigamiPlatform.IntegrationTests;

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    protected readonly CustomWebApplicationFactory _factory;
    protected readonly HttpClient _client;
    protected readonly AppDbContext _dbContext;

    // Giữ Scope để tránh Memory Leak
    private readonly IServiceScope _scope;

    public IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        _scope = factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _scope?.Dispose();
        _client?.Dispose();
    }

    // HELPER CHUNG: Tự động sinh JWT Token thật để vượt qua Auth Middleware
    protected string GenerateJwtToken(string role, Guid userId)
    {
        var config = _factory.Services.GetRequiredService<IConfiguration>();
        var secret = config["JwtSettings:Secret"] ?? config["Jwt:Key"] ?? config["Jwt:Secret"] ?? "YOUR_SUPER_SECRET_KEY_1234567890_CHANGE_ME_IF_NEEDED";
        var issuer = config["JwtSettings:Issuer"] ?? config["Jwt:Issuer"] ?? "OriMate";
        var audience = config["JwtSettings:Audience"] ?? config["Jwt:Audience"] ?? "OriMate";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("id", userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim("role", role)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // HELPER CHUNG: Authenticate nhanh một Role bất kỳ
    protected async Task<Guid> AuthenticateAsAsync(string role)
    {
        var userId = Guid.NewGuid();
        var user = new Domain.Entities.User
        {
            Id = userId,
            Email = $"{role.ToLower()}-{Guid.NewGuid()}@orimate.com",
            PasswordHash = "hashed",
            Status = AccountStatus.Active
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var token = GenerateJwtToken(role, userId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return userId;
    }

    // HELPER CHUNG: Tạo sẵn Category và User để tránh lỗi Khóa ngoại (Foreign Key)
    protected async Task<(int CategoryId, Guid AuthorId)> SeedDefaultPrerequisitesAsync()
    {
        var category = new Domain.Entities.Category { Name = "Category_" + Guid.NewGuid().ToString().Substring(0, 5), IsActive = true };
        _dbContext.Categories.Add(category);

        var authorId = Guid.NewGuid();
        var author = new Domain.Entities.User
        {
            Id = authorId,
            Email = $"author-{Guid.NewGuid()}@orimate.com",
            PasswordHash = "hashed",
            Status = AccountStatus.Active
        };
        _dbContext.Users.Add(author);

        await _dbContext.SaveChangesAsync();
        return (category.Id, authorId);
    }
}