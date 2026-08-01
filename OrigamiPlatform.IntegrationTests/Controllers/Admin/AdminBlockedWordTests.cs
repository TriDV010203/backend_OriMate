using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.Admin;

public class AdminBlockedWordTests : IntegrationTestBase
{
    public AdminBlockedWordTests(CustomWebApplicationFactory factory) : base(factory) { }

    private async Task<string> AuthenticateAsAdminAsync()
    {
        var adminEmail = "admin_blockedword_test@origami.com";
        var rawPassword = "AdminPassword123!";
        var existingAdmin = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (existingAdmin == null)
        {
            var adminUser = new User { Id = Guid.NewGuid(), Email = adminEmail, PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword), Status = AccountStatus.Active, CreatedAt = DateTime.UtcNow };
            await _dbContext.Users.AddAsync(adminUser);
            await _dbContext.UserRoles.AddAsync(new UserRole { UserId = adminUser.Id, Role = UserRoleType.Admin });
            await _dbContext.SaveChangesAsync();
        }
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(adminEmail, rawPassword));
        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return authData!.Token;
    }

    [Fact]
    public async Task CreateBlockedWord_ByAdmin_ShouldPersist()
    {
        var adminToken = await AuthenticateAsAdminAsync();
        var word = "badword123";

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/admin/blocked-words");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        requestMessage.Content = JsonContent.Create(new { Word = word });

        var response = await _client.SendAsync(requestMessage);

        response.IsSuccessStatusCode.Should().BeTrue();
        var wordInDb = await _dbContext.BlockedWords.FirstOrDefaultAsync(b => b.Word == word);
        wordInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task RemoveBlockedWord_ByAdmin_ShouldDelete()
    {
        var adminToken = await AuthenticateAsAdminAsync();
        var blockedWord = new BlockedWord { Word = "tobedeleted", CreatedAt = DateTime.UtcNow };
        await _dbContext.BlockedWords.AddAsync(blockedWord);
        await _dbContext.SaveChangesAsync();

        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/blocked-words/{blockedWord.Id}");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(requestMessage);

        response.IsSuccessStatusCode.Should().BeTrue();
        var wordInDb = await _dbContext.BlockedWords.FirstOrDefaultAsync(b => b.Id == blockedWord.Id);
        wordInDb.Should().BeNull();
    }

    [Fact]
    public async Task CreateBlockedWord_WithEmptyOrNullWord_ShouldReturnBadRequest()
    {
        var adminToken = await AuthenticateAsAdminAsync();
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/admin/blocked-words");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        requestMessage.Content = JsonContent.Create(new { Word = "" });

        var response = await _client.SendAsync(requestMessage);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBlockedWord_WithExcessivelyLongWord_ShouldReturnBadRequest()
    {
        var adminToken = await AuthenticateAsAdminAsync();
        var excessivelyLongWord = new string('x', 500);
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/admin/blocked-words");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        requestMessage.Content = JsonContent.Create(new { Word = excessivelyLongWord });

        var response = await _client.SendAsync(requestMessage);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBlockedWord_DuplicateWord_ShouldReturnConflictOrBadRequest()
    {
        var adminToken = await AuthenticateAsAdminAsync();
        var duplicateWord = "spam_word";

        await _dbContext.BlockedWords.AddAsync(new BlockedWord { Word = duplicateWord, CreatedAt = DateTime.UtcNow });
        await _dbContext.SaveChangesAsync();

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/admin/blocked-words");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        requestMessage.Content = JsonContent.Create(new { Word = duplicateWord });

        var response = await _client.SendAsync(requestMessage);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Conflict, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RemoveBlockedWord_NotFound_ShouldReturnNotFound()
    {
        var adminToken = await AuthenticateAsAdminAsync();
        var nonExistentId = 999999;

        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/blocked-words/{nonExistentId}");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(requestMessage);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBlockedWord_ByAdmin_ShouldGenerateAuditLogEntry()
    {
        var adminToken = await AuthenticateAsAdminAsync();
        var word = "audit_word_test";

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/admin/blocked-words");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        requestMessage.Content = JsonContent.Create(new { Word = word });

        var response = await _client.SendAsync(requestMessage);
        response.IsSuccessStatusCode.Should().BeTrue();

        var auditLogInDb = await _dbContext.AuditLogs
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        auditLogInDb.Should().NotBeNull("Mọi thao tác quản trị của Admin phải được ghi vết vào AuditLog");
    }
}