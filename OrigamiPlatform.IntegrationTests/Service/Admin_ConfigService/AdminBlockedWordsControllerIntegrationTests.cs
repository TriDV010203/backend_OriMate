using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Services.Admin_ConfigService;

public class AdminBlockedWordsControllerIntegrationTests : IntegrationTestBase
{
    public AdminBlockedWordsControllerIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path (Primary success flow for blocked word addition)
    [Fact]
    public async Task AddBlockedWord_AsAdmin_ReturnsSuccess_HappyPath()
    {
        await AuthenticateAsAsync("Admin");
        var wordToAdd = $"badword_{Guid.NewGuid().ToString()[..5]}";
        var req = new { word = wordToAdd };

        var response = await _client.PostAsJsonAsync("/api/admin/blocked-words", req);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("word").GetString().Should().Be(wordToAdd.ToLower());

        _dbContext.ChangeTracker.Clear();
        var dbWord = await _dbContext.BlockedWords.FirstOrDefaultAsync(b => b.Word == wordToAdd.ToLower());
        dbWord.Should().NotBeNull();
    }

    // 🔬 Coverage Technique: Error Path (Unauthorized access scenario)
    [Fact]
    public async Task AddBlockedWord_AsNonAdmin_ReturnsForbidden_ErrorPath()
    {
        await AuthenticateAsAsync("User");
        var req = new { word = "badword" };

        var response = await _client.PostAsJsonAsync("/api/admin/blocked-words", req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // 🔬 Coverage Technique: Error Path (Duplicate entry rejection scenario)
    [Fact]
    public async Task AddBlockedWord_DuplicateWord_ReturnsConflict_ErrorPath()
    {
        await AuthenticateAsAsync("Admin");
        var wordToAdd = $"duplicate_{Guid.NewGuid().ToString()[..5]}";
        await _client.PostAsJsonAsync("/api/admin/blocked-words", new { word = wordToAdd });

        var response = await _client.PostAsJsonAsync("/api/admin/blocked-words", new { word = wordToAdd });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // 🔬 Coverage Technique: Concurrency (Simultaneous requests attempting to add the same blocked word)
    [Fact]
    public async Task AddBlockedWord_ConcurrentDuplicate_OnlyOneSucceeds()
    {
        await AuthenticateAsAsync("Admin");
        var wordToAdd = $"concurrency_{Guid.NewGuid().ToString()[..5]}";
        var req1 = new { word = wordToAdd };
        var req2 = new { word = wordToAdd };

        var task1 = _client.PostAsJsonAsync("/api/admin/blocked-words", req1);
        var task2 = _client.PostAsJsonAsync("/api/admin/blocked-words", req2);

        var responses = await Task.WhenAll(task1, task2);

        responses.Count(r => r.IsSuccessStatusCode).Should().Be(1);
        responses.Count(r => !r.IsSuccessStatusCode).Should().Be(1);
    }

    // 🔬 Coverage Technique: Idempotency & Transaction Boundary (Delete action and second call no-op/not found)
    [Fact]
    public async Task RemoveBlockedWord_ValidId_DeletesSuccessfully_AndIsIdempotent()
    {
        await AuthenticateAsAsync("Admin");
        var blockedWord = new BlockedWord { Word = $"remove_{Guid.NewGuid().ToString()[..5]}", CreatedAt = DateTime.UtcNow };
        _dbContext.BlockedWords.Add(blockedWord);
        await _dbContext.SaveChangesAsync();

        var response1 = await _client.DeleteAsync($"/api/admin/blocked-words/{blockedWord.Id}");
        response1.EnsureSuccessStatusCode();

        _dbContext.ChangeTracker.Clear();
        var dbCheck = await _dbContext.BlockedWords.FindAsync(blockedWord.Id);
        dbCheck.Should().BeNull();

        // Second call verifies idempotent or error handling behaviour on missing resource
        var response2 = await _client.DeleteAsync($"/api/admin/blocked-words/{blockedWord.Id}");
        response2.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}