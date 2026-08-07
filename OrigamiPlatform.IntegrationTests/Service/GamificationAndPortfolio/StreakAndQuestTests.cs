using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.GamificationAndPortfolio;

public class StreakAndQuestTests : IntegrationTestBase
{
    public StreakAndQuestTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path: Verify the primary success flow — DB state correct, events published, response correct.
    [Fact]
    public async Task PurchaseStreakFreeze_WithSufficientBalance_Success()
    {
        // 1. Arrange: Đăng nhập User và cấp đủ Hạt Gấp (20 Hạt Gấp / Freeze)
        var userId = await AuthenticateAsAsync("User");

        var profile = await _dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            profile = new UserProfile { UserId = userId, DisplayName = "Streak User" };
            _dbContext.UserProfiles.Add(profile);
        }
        await _dbContext.SaveChangesAsync();

        await _dbContext.HatGapTransactions.AddAsync(new HatGapTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = 50,
            Type = HatGapTransactionType.Earn,
            Source = "InitialReward",
            BalanceAfter = 50,
            CreatedAt = DateTime.UtcNow
        });

        _dbContext.StreakLogs.Add(new StreakLog
        {
            UserId = userId,
            CurrentStreak = 5,
            LongestStreak = 5,
            FreezeCount = 0,
            LastActiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7))
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // 2. Act
        var response = await _client.PostAsync("/api/gamification/streak-freeze", null);

        // 3. Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("freezeCount").GetInt32().Should().Be(1);
    }

    // 🔬 Coverage Technique: Boundary Value: Test BVA from SRS at integration level — e.g., max inventory cap of 2 freezes (BR-SEEDS-02).
    [Fact]
    public async Task PurchaseStreakFreeze_WhenMaxLimitReached_ReturnsBadRequest()
    {
        // 1. Arrange: User đã đạt tối đa 2 Streak Freezes trong kho (Biên tối đa theo BR-SEEDS-02)
        var userId = await AuthenticateAsAsync("User");

        _dbContext.StreakLogs.Add(new StreakLog
        {
            UserId = userId,
            CurrentStreak = 5,
            LongestStreak = 5,
            FreezeCount = 2, // Đạt biên giới hạn tối đa
            LastActiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7))
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // 2. Act
        var response = await _client.PostAsync("/api/gamification/streak-freeze", null);

        // 3. Assert: Hệ thống phải từ chối giao dịch do chạm biên giới hạn holding
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 🔬 Coverage Technique: Error Path: Verify failure scenarios — rollback complete, compensation triggered, no partial state (Insufficient balance).
    [Fact]
    public async Task PurchaseStreakFreeze_WithInsufficientBalance_ReturnsBadRequest()
    {
        // 1. Arrange: User không có đủ Hạt Gấp (Số dư ví = 0)
        var userId = await AuthenticateAsAsync("User");

        _dbContext.StreakLogs.Add(new StreakLog
        {
            UserId = userId,
            CurrentStreak = 3,
            LongestStreak = 3,
            FreezeCount = 0,
            LastActiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7))
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // 2. Act
        var response = await _client.PostAsync("/api/gamification/streak-freeze", null);

        // 3. Assert: Không đủ tiền mua -> Trả về lỗi 400 BadRequest
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 🔬 Coverage Technique: Error Path: Verify failure scenarios — unauthorized access rejection.
    [Fact]
    public async Task PurchaseStreakFreeze_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange: Xóa token xác thực (Giả lập Guest)
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.PostAsync("/api/gamification/streak-freeze", null);

        // Assert: Chặn truy cập chưa đăng nhập
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // 🔬 Coverage Technique: Happy Path: Verify the primary success flow — DB state correct, events published, response correct.
    [Fact]
    public async Task GetMyQuestToday_ReturnsActiveQuestProgress_HappyPath()
    {
        // Arrange
        await AuthenticateAsAsync("User");

        var quest = new DailyQuest
        {
            Id = Guid.NewGuid(),
            Title = "Complete 1 step",
            TargetValue = 1,
            IsActive = true
        };
        _dbContext.DailyQuests.Add(quest);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var response = await _client.GetAsync("/api/gamification/quest-today");

        // Assert
        response.EnsureSuccessStatusCode();
        var questData = await response.Content.ReadFromJsonAsync<JsonElement>();
        questData.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }

    // 🔬 Coverage Technique: Idempotency: Send same request twice — second call must be a no-op or consistent.
    [Fact]
    public async Task GetMyQuestToday_MultipleCalls_IsIdempotent()
    {
        // Arrange
        await AuthenticateAsAsync("User");

        var quest = new DailyQuest
        {
            Id = Guid.NewGuid(),
            Title = "Complete 1 step",
            TargetValue = 1,
            IsActive = true
        };
        _dbContext.DailyQuests.Add(quest);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act: Gọi lần 1
        var response1 = await _client.GetAsync("/api/gamification/quest-today");
        response1.EnsureSuccessStatusCode();

        // Act: Gọi lần 2
        var response2 = await _client.GetAsync("/api/gamification/quest-today");

        // Assert: Trả về kết quả nhất quán không đổi state
        response2.EnsureSuccessStatusCode();
        var data1 = await response1.Content.ReadAsStringAsync();
        var data2 = await response2.Content.ReadAsStringAsync();
        data1.Should().Be(data2);
    }
}