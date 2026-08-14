using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Service.Challenges;

public class DailyChallengeTests : IntegrationTestBase
{
    public DailyChallengeTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path: Verify user can submit a photo entry to today's active Daily Challenge (FT-34).
    [Fact]
    public async Task SubmitTodayChallenge_ActiveChallenge_Succeeds()
    {
        // 1. Arrange: Tạo một Active Daily Challenge cho ngày hôm nay (GMT+7)
        var userId = await AuthenticateAsAsync("User");
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var tutorialId = Guid.NewGuid();

        var prereq = await SeedDefaultPrerequisitesAsync();
        _dbContext.Tutorials.Add(new Tutorial
        {
            Id = tutorialId,
            Title = "Challenge Tutorial",
            Slug = "chal-tut-" + Guid.NewGuid(),
            CategoryId = prereq.CategoryId,
            AuthorId = prereq.AuthorId,
            Status = TutorialStatus.Published,
            PublishedAt = DateTime.UtcNow
        });

        _dbContext.DailyChallenges.Add(new DailyChallenge
        {
            Id = Guid.NewGuid(),
            ChallengeDate = today,
            TutorialId = tutorialId,
            Status = DailyChallengeStatus.Active,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var request = new
        {
            PhotoUrl = "https://example.com/challenge_submission.jpg",
            Note = "Bài nộp thử thách ngày của tôi!"
        };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/daily-challenge/today/submit", request);

        // 3. Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("photoUrl").GetString().Should().Be(request.PhotoUrl);

        var subInDb = await _dbContext.DailyChallengeSubmissions
            .FirstOrDefaultAsync(s => s.UserId == userId);
        subInDb.Should().NotBeNull();
    }

    // 🔬 Coverage Technique: Error Path: Verify duplicate submission for today's challenge is rejected (BR-CHAL-02).
    [Fact]
    public async Task SubmitTodayChallenge_AlreadySubmitted_ReturnsBadRequest()
    {
        // 1. Arrange: User đã nộp bài trước đó cho thử thách ngày hôm nay
        var userId = await AuthenticateAsAsync("User");
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        // Tránh trùng lặp Unique Index nếu test trước đã add ngày này, ta có thể dùng FirstOrDefault kiểm tra
        var existingChallenge = await _dbContext.DailyChallenges.FirstOrDefaultAsync(c => c.ChallengeDate == targetDate);
        Guid challengeId;
        var tutorialId = Guid.NewGuid();

        var prereq = await SeedDefaultPrerequisitesAsync();
        _dbContext.Tutorials.Add(new Tutorial
        {
            Id = tutorialId,
            Title = "Challenge Tutorial 2",
            Slug = "chal-tut-2-" + Guid.NewGuid(),
            CategoryId = prereq.CategoryId,
            AuthorId = prereq.AuthorId,
            Status = TutorialStatus.Published,
            PublishedAt = DateTime.UtcNow
        });

        if (existingChallenge == null)
        {
            challengeId = Guid.NewGuid();
            _dbContext.DailyChallenges.Add(new DailyChallenge
            {
                Id = challengeId,
                ChallengeDate = targetDate,
                TutorialId = tutorialId,
                Status = DailyChallengeStatus.Active,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            challengeId = existingChallenge.Id;
            existingChallenge.Status = DailyChallengeStatus.Active;
        }

        _dbContext.DailyChallengeSubmissions.Add(new DailyChallengeSubmission
        {
            Id = Guid.NewGuid(),
            DailyChallengeId = challengeId,
            UserId = userId,
            PhotoUrl = "https://example.com/first.jpg",
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var request = new
        {
            PhotoUrl = "https://example.com/second.jpg",
            Note = "Cố tình nộp lần hai."
        };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/daily-challenge/today/submit", request);

        // 3. Assert: Phải từ chối với mã 400 BadRequest (BR-CHAL-02)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 🔬 Coverage Technique: Happy Path: Verify Admin/Manager can schedule a Daily Challenge successfully (FT-34).
    [Fact]
    public async Task AdminScheduleChallenge_ValidData_Succeeds()
    {
        // 1. Arrange: Đăng nhập với quyền Admin/Manager
        await AuthenticateAsAsync("Admin");
        var tutorialId = Guid.NewGuid();
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7).AddDays(1));

        var prereq = await SeedDefaultPrerequisitesAsync();
        _dbContext.Tutorials.Add(new Tutorial
        {
            Id = tutorialId,
            Title = "Scheduled Tutorial",
            Slug = "sched-tut-" + Guid.NewGuid(),
            CategoryId = prereq.CategoryId,
            AuthorId = prereq.AuthorId,
            Status = TutorialStatus.Published,
            IsOfficial = true,
            PublishedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var request = new
        {
            ChallengeDate = targetDate,
            TutorialId = tutorialId
        };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/daily-challenge/admin/schedule", request);

        // 3. Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("status").GetString().Should().Be("Scheduled");
    }

    // 🔬 Coverage Technique: Error Path: Verify regular user cannot access admin schedule endpoint (Security / 403 Forbidden).
    [Fact]
    public async Task AdminScheduleChallenge_ByRegularUser_ReturnsForbidden()
    {
        // 1. Arrange: Đăng nhập quyền User thường
        await AuthenticateAsAsync("User");
        var request = new
        {
            ChallengeDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7).AddDays(1)),
            TutorialId = Guid.NewGuid()
        };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/daily-challenge/admin/schedule", request);

        // 3. Assert: Phải bị chặn với mã 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}