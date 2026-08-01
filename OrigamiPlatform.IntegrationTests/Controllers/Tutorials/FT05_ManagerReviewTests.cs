//using System.IdentityModel.Tokens.Jwt;
//using System.Net;
//using System.Net.Http.Headers;
//using System.Net.Http.Json;
//using System.Security.Claims;
//using System.Text;
//using FluentAssertions;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.IdentityModel.Tokens;
//using OrigamiPlatform.Domain.Enums;
//using Xunit;

//namespace OrigamiPlatform.IntegrationTests.Tutorials;

//public class FT05_ManagerReviewTests : IntegrationTestBase
//{
//    private readonly Guid _managerId = Guid.NewGuid();

//    public FT05_ManagerReviewTests(CustomWebApplicationFactory factory) : base(factory) { }

//    private string GenerateValidJwtToken(string role, Guid userId)
//    {
//        var config = _factory.Services.GetRequiredService<IConfiguration>();
//        var secret = config["JwtSettings:Secret"] ?? config["Jwt:Key"] ?? config["Jwt:Secret"] ?? "YOUR_SUPER_SECRET_KEY_1234567890_CHANGE_ME_IF_NEEDED";
//        var issuer = config["JwtSettings:Issuer"] ?? config["Jwt:Issuer"] ?? "OriMate";
//        var audience = config["JwtSettings:Audience"] ?? config["Jwt:Audience"] ?? "OriMate";

//        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
//        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

//        var claims = new[]
//        {
//            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
//            new Claim("id", userId.ToString()),
//            new Claim(ClaimTypes.Role, role),
//            new Claim("role", role)
//        };

//        var token = new JwtSecurityToken(
//            issuer: issuer,
//            audience: audience,
//            claims: claims,
//            expires: DateTime.Now.AddHours(1),
//            signingCredentials: creds
//        );

//        return new JwtSecurityTokenHandler().WriteToken(token);
//    }

//    private async Task AuthenticateManagerAsync()
//    {
//        // Tạo Manager user trong DB để thỏa mãn các ràng buộc nếu có
//        var managerUser = new Domain.Entities.User
//        {
//            Id = _managerId,
//            Email = $"manager-{Guid.NewGuid()}@orimate.com",
//            PasswordHash = "hashed",
//            Status = AccountStatus.Active
//        };
//        _dbContext.Users.Add(managerUser);
//        await _dbContext.SaveChangesAsync();

//        var token = GenerateValidJwtToken("Manager", _managerId);
//        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
//    }

//    private async Task<(int CategoryId, Guid AuthorId)> SeedPrerequisitesAsync()
//    {
//        var category = new Domain.Entities.Category { Name = "General " + Guid.NewGuid().ToString().Substring(0, 5), IsActive = true };
//        _dbContext.Categories.Add(category);

//        var authorId = Guid.NewGuid();
//        var author = new Domain.Entities.User
//        {
//            Id = authorId,
//            Email = $"author-{Guid.NewGuid()}@orimate.com",
//            PasswordHash = "hashed",
//            Status = AccountStatus.Active
//        };
//        _dbContext.Users.Add(author);

//        await _dbContext.SaveChangesAsync();
//        return (category.Id, authorId);
//    }

//    [Fact]
//    public async Task AC01_ManagerPublish_ChangesStatus()
//    {
//        // Arrange
//        await AuthenticateManagerAsync();
//        var (categoryId, authorId) = await SeedPrerequisitesAsync();

//        var tutId = Guid.NewGuid();
//        var uniqueSlug = $"pending-tut-{tutId}";
//        _dbContext.Tutorials.Add(new Domain.Entities.Tutorial
//        {
//            Id = tutId,
//            Title = "Pending Tut " + tutId,
//            Slug = uniqueSlug,
//            Status = TutorialStatus.PendingManagerReview,
//            CategoryId = categoryId,
//            AuthorId = authorId
//        });
//        await _dbContext.SaveChangesAsync();

//        // Act
//        var response = await _client.PutAsync($"/api/tutorials/{tutId}/publish", null);
//        var errorContent = await response.Content.ReadAsStringAsync();

//        // Assert
//        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Publish failed: {errorContent}");

//        var tut = await _dbContext.Tutorials.FindAsync(tutId);
//        if (tut != null) await _dbContext.Entry(tut).ReloadAsync();
//        tut!.Status.Should().Be(TutorialStatus.Published);
//    }

//    [Fact]
//    public async Task NAC01_BV01_ManagerReject_WithShortReason_ReturnsBadRequest()
//    {
//        // Arrange
//        await AuthenticateManagerAsync();
//        var (categoryId, authorId) = await SeedPrerequisitesAsync();

//        var tutId = Guid.NewGuid();
//        var uniqueSlug = $"pending-tut-2-{tutId}";
//        _dbContext.Tutorials.Add(new Domain.Entities.Tutorial
//        {
//            Id = tutId,
//            Title = "Pending Tut 2 " + tutId,
//            Slug = uniqueSlug,
//            Status = TutorialStatus.PendingManagerReview,
//            CategoryId = categoryId,
//            AuthorId = authorId
//        });
//        await _dbContext.SaveChangesAsync();

//        var request = new { Reason = "Too short" }; // Boundary < 10 chars (BV-01)

//        // Act
//        var response = await _client.PutAsJsonAsync($"/api/tutorials/{tutId}/reject", request);
//        var errorContent = await response.Content.ReadAsStringAsync();

//        // Assert
//        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"Expected 400 but got {response.StatusCode}. Response: {errorContent}");

//        var tut = await _dbContext.Tutorials.FindAsync(tutId);
//        if (tut != null) await _dbContext.Entry(tut).ReloadAsync();
//        tut!.Status.Should().Be(TutorialStatus.PendingManagerReview);
//    }
//}
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Tutorials;

public class FT05_ManagerReviewTests : IntegrationTestBase
{
    public FT05_ManagerReviewTests(CustomWebApplicationFactory factory) : base(factory) { }

    // [Happy Path] & [Audit Log] - AC-01, AC-03
    [Fact]
    public async Task HappyPath_ManagerPublish_ChangesStatus_And_CreatesHistory()
    {
        var managerId = await AuthenticateAsAsync("Manager");
        var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();

        var tutId = Guid.NewGuid();
        _dbContext.Tutorials.Add(new Domain.Entities.Tutorial
        {
            Id = tutId,
            Title = "Pending Tut",
            Slug = $"pt-{tutId}",
            Status = TutorialStatus.PendingManagerReview,
            CategoryId = categoryId,
            AuthorId = authorId
        });
        await _dbContext.SaveChangesAsync();

        var response = await _client.PutAsync($"/api/tutorials/{tutId}/publish", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tut = await _dbContext.Tutorials.FindAsync(tutId);
        await _dbContext.Entry(tut!).ReloadAsync();
        tut!.Status.Should().Be(TutorialStatus.Published);

        // Đảm bảo Audit/History được sinh ra (AC-03)
        var historyCount = await _dbContext.TutorialReviewHistories.CountAsync(h => h.TutorialId == tutId);
        historyCount.Should().Be(1);
    }

    // [BVA] - Kiểm thử độ dài lý do từ chối (BV-01: 10 chars)
    [Theory]
    [InlineData("Too short", HttpStatusCode.BadRequest)] // 9 chars -> Lỗi
    [InlineData("Ten chars.", HttpStatusCode.OK)]        // 10 chars -> Thành công
    public async Task BV01_ManagerReject_ReasonLengthBoundaries(string reason, HttpStatusCode expectedStatus)
    {
        await AuthenticateAsAsync("Manager");
        var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();

        var tutId = Guid.NewGuid();
        _dbContext.Tutorials.Add(new Domain.Entities.Tutorial
        {
            Id = tutId,
            Title = "Pending Tut",
            Slug = $"pt-bv-{tutId}",
            Status = TutorialStatus.PendingManagerReview,
            CategoryId = categoryId,
            AuthorId = authorId
        });
        await _dbContext.SaveChangesAsync();

        var request = new { Reason = reason };
        var response = await _client.PutAsJsonAsync($"/api/tutorials/{tutId}/reject", request);

        response.StatusCode.Should().Be(expectedStatus);
    }

    // [Error Path] & [State Transition] - Publish thẳng một bài Draft (Bypass luồng)
    [Fact]
    public async Task ErrorPath_PublishDraftTutorial_ReturnsBadRequest()
    {
        await AuthenticateAsAsync("Manager");
        var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();

        var tutId = Guid.NewGuid();
        _dbContext.Tutorials.Add(new Domain.Entities.Tutorial
        {
            Id = tutId,
            Title = "Draft Tut",
            Slug = $"draft-{tutId}",
            Status = TutorialStatus.Draft,
            CategoryId = categoryId,
            AuthorId = authorId
        });
        await _dbContext.SaveChangesAsync();

        var response = await _client.PutAsync($"/api/tutorials/{tutId}/publish", null);

        // Assert: Trạng thái không hợp lệ để Publish
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // [Idempotency] - Publish một bài đã Published
    [Fact]
    public async Task Idempotency_PublishAlreadyPublishedTutorial_ReturnsBadRequest()
    {
        await AuthenticateAsAsync("Manager");
        var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();

        var tutId = Guid.NewGuid();
        _dbContext.Tutorials.Add(new Domain.Entities.Tutorial
        {
            Id = tutId,
            Title = "Pub Tut",
            Slug = $"pub-{tutId}",
            Status = TutorialStatus.Published,
            CategoryId = categoryId,
            AuthorId = authorId
        });
        await _dbContext.SaveChangesAsync();

        var response = await _client.PutAsync($"/api/tutorials/{tutId}/publish", null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}