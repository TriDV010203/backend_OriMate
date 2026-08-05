using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.ClanAndChallenges;

public class DailyChallengeTests : IntegrationTestBase
{
    public DailyChallengeTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task SubmitDailyChallenge_FirstTime_CreatesSubmissionSuccessfully() // [Happy Path]
    {
        // 1. Arrange
        var userId = await AuthenticateAsAsync("User");
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        var streakLog = await _dbContext.Set<ChallengeStreakLog>().FirstOrDefaultAsync(s => s.UserId == userId);
        if (streakLog == null)
        {
            streakLog = new ChallengeStreakLog
            {
                UserId = userId,
                CurrentStreak = 0,
                LongestStreak = 0,
                FreezeCount = 0,
                LastSubmissionDate = null
            };
            _dbContext.Set<ChallengeStreakLog>().Add(streakLog);
        }

        // Kiểm tra xem challenge hôm nay đã tồn tại chưa (tránh trùng Unique Index)
        var challenge = await _dbContext.DailyChallenges.FirstOrDefaultAsync(c => c.ChallengeDate == today);
        Guid challengeId;
        if (challenge == null)
        {
            var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();
            var tutorialId = Guid.NewGuid();
            _dbContext.Tutorials.Add(new Tutorial
            {
                Id = tutorialId,
                Title = "Thử thách hạc giấy 1",
                Slug = $"thu-thach-1-{Guid.NewGuid()}",
                Status = TutorialStatus.Published,
                CategoryId = categoryId,
                AuthorId = authorId
            });

            challengeId = Guid.NewGuid();
            _dbContext.DailyChallenges.Add(new DailyChallenge
            {
                Id = challengeId,
                TutorialId = tutorialId,
                Status = DailyChallengeStatus.Active,
                ChallengeDate = today
            });
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            challengeId = challenge.Id;
        }

        // Đảm bảo user chưa có bài nộp nào cho ngày hôm nay
        var existingSub = await _dbContext.DailyChallengeSubmissions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.DailyChallengeId == challengeId);
        if (existingSub != null)
        {
            _dbContext.DailyChallengeSubmissions.Remove(existingSub);
            await _dbContext.SaveChangesAsync();
        }

        _dbContext.ChangeTracker.Clear();

        var request = new
        {
            PhotoUrl = "https://fake-cloudinary.com/hac.jpg",
            Note = "Hoàn thành thử thách ngày hôm nay!"
        };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/daily-challenge/today/submit", request);

        // 3. Assert
        response.EnsureSuccessStatusCode();

        _dbContext.ChangeTracker.Clear();
        var submission = await _dbContext.DailyChallengeSubmissions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.DailyChallengeId == challengeId);

        submission.Should().NotBeNull("Hệ thống phải lưu lại bài nộp (Submission) của User vào DB.");
    }

    [Fact]
    public async Task SubmitDailyChallenge_SecondTime_ReturnsBadRequest() // [Error Path / Idempotency / BR-CHAL-02]
    {
        // 1. Arrange
        var userId = await AuthenticateAsAsync("User");
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        var streakLog = await _dbContext.Set<ChallengeStreakLog>().FirstOrDefaultAsync(s => s.UserId == userId);
        if (streakLog == null)
        {
            streakLog = new ChallengeStreakLog
            {
                UserId = userId,
                CurrentStreak = 0,
                LongestStreak = 0,
                FreezeCount = 0,
                LastSubmissionDate = null
            };
            _dbContext.Set<ChallengeStreakLog>().Add(streakLog);
        }

        // Lấy hoặc tạo challenge đúng ngày HÔM NAY (vì endpoint gọi /today/submit)
        var challenge = await _dbContext.DailyChallenges.FirstOrDefaultAsync(c => c.ChallengeDate == today);
        Guid challengeId;
        if (challenge == null)
        {
            var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();
            var tutorialId = Guid.NewGuid();
            _dbContext.Tutorials.Add(new Tutorial
            {
                Id = tutorialId,
                Title = "Thử thách hạc giấy 2",
                Slug = $"thu-thach-2-{Guid.NewGuid()}",
                Status = TutorialStatus.Published,
                CategoryId = categoryId,
                AuthorId = authorId
            });

            challengeId = Guid.NewGuid();
            _dbContext.DailyChallenges.Add(new DailyChallenge
            {
                Id = challengeId,
                TutorialId = tutorialId,
                Status = DailyChallengeStatus.Active,
                ChallengeDate = today
            });
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            challengeId = challenge.Id;
        }

        // Giả lập User đã nộp bài lần 1 trước đó cho challenge HÔM NAY
        var existingSub = await _dbContext.DailyChallengeSubmissions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.DailyChallengeId == challengeId);
        if (existingSub == null)
        {
            _dbContext.DailyChallengeSubmissions.Add(new DailyChallengeSubmission
            {
                Id = Guid.NewGuid(),
                DailyChallengeId = challengeId,
                UserId = userId,
                PhotoUrl = "https://fake-cloudinary.com/hac-1.jpg",
                CreatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();
        }

        _dbContext.ChangeTracker.Clear();

        var request = new
        {
            PhotoUrl = "https://fake-cloudinary.com/hac-2.jpg",
            Note = "Cố tình nộp lần thứ hai"
        };

        // 2. Act
        // Gọi API nộp bài cho ngày hôm nay (lần thứ hai)
        var response = await _client.PostAsJsonAsync("/api/daily-challenge/today/submit", request);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Hệ thống vi phạm quy tắc BR-CHAL-02 vì cho phép 1 user nộp 2 bài trong cùng 1 chu kỳ.");
    }

    [Fact]
    public async Task GetTodayChallenge_WithoutAuthentication_ReturnsSuccess() // [Happy Path / Public Access]
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        var existing = await _dbContext.DailyChallenges.FirstOrDefaultAsync(c => c.ChallengeDate == today);
        if (existing == null)
        {
            var (categoryId, authorId) = await SeedDefaultPrerequisitesAsync();
            var tutorialId = Guid.NewGuid();
            _dbContext.Tutorials.Add(new Tutorial
            {
                Id = tutorialId,
                Title = "Thử thách hôm nay",
                Slug = $"thu-thach-today-{Guid.NewGuid()}",
                Status = TutorialStatus.Published,
                CategoryId = categoryId,
                AuthorId = authorId
            });

            _dbContext.DailyChallenges.Add(new DailyChallenge
            {
                Id = Guid.NewGuid(),
                TutorialId = tutorialId,
                Status = DailyChallengeStatus.Active,
                ChallengeDate = today
            });
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
        }

        _client.DefaultRequestHeaders.Authorization = null;

        // 2. Act
        var response = await _client.GetAsync("/api/daily-challenge/today");

        // 3. Assert
        response.EnsureSuccessStatusCode();
    }
}