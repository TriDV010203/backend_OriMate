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

//public class FT04_TutorialAuthoringTests : IntegrationTestBase
//{
//    private readonly Guid _creatorId = Guid.NewGuid();

//    public FT04_TutorialAuthoringTests(CustomWebApplicationFactory factory) : base(factory) { }

//    private string GenerateValidJwtToken(string role)
//    {
//        var config = _factory.Services.GetRequiredService<IConfiguration>();
//        var secret = config["JwtSettings:Secret"] ?? config["Jwt:Key"] ?? config["Jwt:Secret"] ?? "YOUR_SUPER_SECRET_KEY_1234567890_CHANGE_ME_IF_NEEDED";
//        var issuer = config["JwtSettings:Issuer"] ?? config["Jwt:Issuer"] ?? "OriMate";
//        var audience = config["JwtSettings:Audience"] ?? config["Jwt:Audience"] ?? "OriMate";

//        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
//        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

//        var claims = new[]
//        {
//            new Claim(ClaimTypes.NameIdentifier, _creatorId.ToString()),
//            new Claim("id", _creatorId.ToString()),
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

//    private async Task AuthenticateAsync(string role = "Creator")
//    {
//        var user = new Domain.Entities.User
//        {
//            Id = _creatorId,
//            Email = $"test-{Guid.NewGuid()}@orimate.com",
//            PasswordHash = "hashed",
//            Status = AccountStatus.Active
//        };
//        _dbContext.Users.Add(user);
//        await _dbContext.SaveChangesAsync();

//        var validToken = GenerateValidJwtToken(role);
//        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
//    }

//    [Fact]
//    public async Task AC01_AC02_SubmitValidTutorial_CreatesDraft_And_CanSubmitForReview()
//    {
//        // Arrange
//        await AuthenticateAsync();
//        var category = new Domain.Entities.Category { Name = "Animals", IsActive = true };
//        _dbContext.Categories.Add(category);
//        await _dbContext.SaveChangesAsync();

//        var request = new
//        {
//            Title = "Valid Paper Crane 123",
//            Description = "Valid description exceeding 20 characters.",
//            CategoryId = category.Id,
//            Difficulty = "Intermediate",
//            Type = "Free",
//            CoverImageUrl = "https://fake-cloudinary.com/test-image.jpg",
//            Steps = Enumerable.Range(1, 5).Select(i => new
//            {
//                Description = $"This is a valid and detailed instruction for step number {i}.",
//                ImageUrl = $"https://fake-cloudinary.com/step{i}.jpg"
//            }).ToArray()
//        };

//        // Act 1: Tạo Draft (POST /api/tutorials) -> Trả về 201 Created
//        var draftResponse = await _client.PostAsJsonAsync("/api/tutorials", request);
//        var responseContent = await draftResponse.Content.ReadAsStringAsync();
//        draftResponse.StatusCode.Should().Be(HttpStatusCode.Created, $"Because creation failed: {responseContent}");

//        var draft = await _dbContext.Tutorials.FirstAsync(t => t.Title == request.Title);
//        draft.Status.Should().Be(TutorialStatus.Draft);

//        // Act 2: Gửi duyệt (PUT /api/tutorials/{id}/submit)
//        var submitResponse = await _client.PutAsync($"/api/tutorials/{draft.Id}/submit", null);
//        var submitError = await submitResponse.Content.ReadAsStringAsync();

//        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"Submit failed with error: {submitError}");

//        // FIX LỖI CACHE: Ép Entity Framework đọc dữ liệu mới nhất từ DB thay vì dùng giá trị cũ trong bộ nhớ
//        var updatedDraft = await _dbContext.Tutorials.FindAsync(draft.Id);
//        if (updatedDraft != null)
//        {
//            await _dbContext.Entry(updatedDraft).ReloadAsync();
//        }

//        updatedDraft!.Status.Should().Be(TutorialStatus.PendingManagerReview);
//    }

//    [Fact]
//    public async Task NAC01_BV03_CreateTutorial_WithTwoSteps_ReturnsBadRequestOnSubmit()
//    {
//        // Arrange
//        await AuthenticateAsync();
//        var category = new Domain.Entities.Category { Name = "Plants", IsActive = true };
//        _dbContext.Categories.Add(category);
//        await _dbContext.SaveChangesAsync();

//        var request = new
//        {
//            Title = "Invalid Step Count Tutorial",
//            Description = "Valid description here.",
//            CategoryId = category.Id,
//            Difficulty = "Intermediate",
//            Type = "Free",
//            CoverImageUrl = "https://fake-cloudinary.com/test-image.jpg",
//            Steps = Enumerable.Range(1, 2).Select(i => new { Description = $"Step {i} text.", ImageUrl = $"step{i}.jpg" }).ToArray()
//        };

//        // Bước 1: Tạo Draft thành công (201 Created)
//        var createResponse = await _client.PostAsJsonAsync("/api/tutorials", request);
//        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

//        var draft = await _dbContext.Tutorials.FirstAsync(t => t.Title == request.Title);

//        // Bước 2: Gửi duyệt bài học có 2 bước (vi phạm giới hạn 3-30 bước) -> Bị Backend từ chối với 400 Bad Request
//        var submitResponse = await _client.PutAsync($"/api/tutorials/{draft.Id}/submit", null);

//        // Assert
//        submitResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

//        var dbDraft = await _dbContext.Tutorials.FindAsync(draft.Id);
//        if (dbDraft != null)
//        {
//            await _dbContext.Entry(dbDraft).ReloadAsync();
//        }
//        dbDraft!.Status.Should().Be(TutorialStatus.Draft);
//    }
//}
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Tutorials;

public class FT04_TutorialAuthoringTests : IntegrationTestBase
{
    public FT04_TutorialAuthoringTests(CustomWebApplicationFactory factory) : base(factory) { }

    // [Happy Path] & [State Transition]
    [Fact]
    public async Task AC01_AC02_SubmitValidTutorial_CreatesDraft_And_TransitionsToPending()
    {
        var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();
        var token = GenerateJwtToken("Creator", authorId);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            Title = "Valid Paper Crane 123",
            Description = "Valid description exceeding 20 characters.",
            CategoryId = categoryId,
            Difficulty = "Intermediate",
            Type = "Free",
            CoverImageUrl = "https://fake-cloudinary.com/test-image.jpg",
            Steps = Enumerable.Range(1, 5).Select(i => new { Description = $"Step {i}", ImageUrl = "url.jpg" }).ToArray()
        };

        var draftResponse = await _client.PostAsJsonAsync("/api/tutorials", request);
        draftResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var draft = await _dbContext.Tutorials.FirstAsync(t => t.Title == request.Title);

        var submitResponse = await _client.PutAsync($"/api/tutorials/{draft.Id}/submit", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await _dbContext.Entry(draft).ReloadAsync();
        draft.Status.Should().Be(TutorialStatus.PendingManagerReview);
    }

    // [BVA] - Kiểm thử giá trị biên số lượng bước (BR-TUT-05: 3-30 bước)
    [Theory]
    [InlineData(3, HttpStatusCode.Created)]  // Cận dưới hợp lệ
    [InlineData(30, HttpStatusCode.Created)] // Cận trên hợp lệ
    [InlineData(2, HttpStatusCode.BadRequest)] // Vi phạm cận dưới
    [InlineData(31, HttpStatusCode.BadRequest)] // Vi phạm cận trên
    public async Task BV03_CreateTutorial_StepCountBoundaries_ValidatedCorrectly(int stepCount, HttpStatusCode expectedCreateStatus)
    {
        var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();
        var token = GenerateJwtToken("Creator", authorId);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            Title = $"Boundary Test {stepCount} Steps",
            Description = "Valid description exceeding 20 chars.",
            CategoryId = categoryId,
            Difficulty = "Beginner",
            Type = "Free",
            Steps = Enumerable.Range(1, stepCount).Select(i => new { Description = $"Step {i}", ImageUrl = "url.jpg" }).ToArray()
        };

        var response = await _client.PostAsJsonAsync("/api/tutorials", request);

        // Lưu ý: Nếu BE cho phép lưu Draft với số bước sai, nhưng chặn lúc Submit, ta cần điều chỉnh Assert cho phù hợp.
        // Giả định BE dùng FluentValidation chặn ngay từ lúc Create theo chuẩn BVA:
        response.StatusCode.Should().Be(expectedCreateStatus);
    }

    // [Error Path] - Vi phạm từ cấm (BR-COMM-01)
    [Fact]
    public async Task ErrorPath_CreateTutorial_WithBlockedWord_ReturnsBadRequest()
    {
        var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();

        // Seed từ cấm
        _dbContext.BlockedWords.Add(new Domain.Entities.BlockedWord { Word = "badword" });
        await _dbContext.SaveChangesAsync();

        var token = GenerateJwtToken("Creator", authorId);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            Title = "Tutorial with badword inside",
            Description = "Valid description here.",
            CategoryId = categoryId,
            Difficulty = "Beginner",
            Type = "Free",
            Steps = new[] { new { Description = "Step 1", ImageUrl = "url.jpg" } }
        };

        var response = await _client.PostAsJsonAsync("/api/tutorials", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}