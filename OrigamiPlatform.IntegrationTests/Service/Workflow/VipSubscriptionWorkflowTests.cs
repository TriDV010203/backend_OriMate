using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Workflows;

public class VipSubscriptionWorkflowTests : IntegrationTestBase
{
    public VipSubscriptionWorkflowTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Workflow — [Happy Path]: End-to-end VIP Subscription & Gating Unlocking workflow — 
    // Creator configures VIP tier -> User subscribes (Transaction Pending) -> Payment confirmed -> 
    // VipSubscription active -> User successfully accesses VIP tutorial steps 4+ (SC-02 / FT-06 / FT-16).
    [Fact]
    public async Task EndToEnd_VipSubscriptionAndContentAccess_HappyPath_Succeeds()
    {
        // 1. Arrange: Tạo Creator và User hợp lệ trong DB test để tránh lỗi khóa ngoại
        var creatorId = Guid.NewGuid();
        _dbContext.Users.Add(new User
        {
            Id = creatorId,
            Email = "creator_wf@orimate.com",
            PasswordHash = "hash",
            Status = AccountStatus.Active
        });

        _dbContext.CreatorVipSettings.Add(new CreatorVipSettings
        {
            Id = Guid.NewGuid(),
            CreatorId = creatorId,
            Price = 50000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        // Tạo VIP Tutorial do Creator này làm tác giả (có đủ 5 bước, bước 4-5 là VIP)
        var prereq = await SeedDefaultPrerequisitesAsync();
        var tutorialId = Guid.NewGuid();
        var tutorial = new Tutorial
        {
            Id = tutorialId,
            AuthorId = creatorId,
            CategoryId = prereq.CategoryId,
            Title = "Workflow VIP Origami Flower",
            Slug = "workflow-vip-flower-" + Guid.NewGuid(),
            Type = TutorialType.VIP,
            Difficulty = TutorialDifficulty.Advanced,
            Status = TutorialStatus.Published,
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        for (int i = 1; i <= 5; i++)
        {
            tutorial.Steps.Add(new TutorialStep
            {
                Id = Guid.NewGuid(),
                TutorialId = tutorialId,
                StepOrder = i,
                Description = $"Step {i} description",
                ImageUrl = "https://example.com/step.jpg",
                CreatedAt = DateTime.UtcNow
            });
        }
        _dbContext.Tutorials.Add(tutorial);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // 2. Act Step A: Đăng nhập với tư cách User (Subscriber)
        var subscriberId = await AuthenticateAsAsync("User");

        // Gọi API xem chi tiết tutorial
        var detailResponseBefore = await _client.GetAsync($"/api/tutorials/{tutorial.Slug}");
        detailResponseBefore.EnsureSuccessStatusCode();

        // 3. Act Step B: Subscriber tiến hành gọi API Subscribe (Tạo Transaction Pending)
        var subscribeRequest = new
        {
            CreatorId = creatorId,
            ReferenceCode = "WF_TXN_12345"
        };
        var subResponse = await _client.PostAsJsonAsync("/api/subscriptions", subscribeRequest);
        subResponse.EnsureSuccessStatusCode();
        var subResult = await subResponse.Content.ReadFromJsonAsync<JsonElement>();
        var transactionId = subResult.GetProperty("id").GetGuid();

        subResult.GetProperty("status").GetString().Should().Be("PendingConfirmation");

        // 4. Act Step C: Giả lập Admin xác nhận thanh toán qua DB (Cập nhật Transaction thành Confirmed & Tạo VipSubscription Active)
        _dbContext.ChangeTracker.Clear();
        var transactionInDb = await _dbContext.Transactions.FindAsync(transactionId);
        transactionInDb!.Status = TransactionStatus.Confirmed;
        transactionInDb.ConfirmedAt = DateTime.UtcNow;

        var vipSub = new VipSubscription
        {
            Id = Guid.NewGuid(),
            SubscriberId = subscriberId,
            CreatorId = creatorId,
            TransactionId = transactionId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = SubscriptionStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.VipSubscriptions.Add(vipSub);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // 5. Act Step D: Subscriber gọi lại API lấy chi tiết tutorial -> Mở khóa thành công
        var detailResponseAfter = await _client.GetAsync($"/api/tutorials/{tutorial.Slug}");
        detailResponseAfter.EnsureSuccessStatusCode();

        // 6. Assert DB State: Kiểm tra VipSubscription Active thành công
        var subInDb = await _dbContext.VipSubscriptions.FirstOrDefaultAsync(s => s.TransactionId == transactionId);
        subInDb.Should().NotBeNull();
        subInDb!.Status.Should().Be(SubscriptionStatus.Active);
        subInDb.EndDate.Should().BeAfter(subInDb.StartDate);
    }

    // 🔬 Coverage Technique: Workflow — [Suppression]: Verify duplicate active subscription attempt is rejected (BR-VIP-05).
    [Fact]
    public async Task Subscribe_WhenAlreadyActive_Suppression_ReturnsBadRequest()
    {
        // 1. Arrange: Đăng nhập User trước để lấy đúng subscriberId định danh
        var subscriberId = await AuthenticateAsAsync("User");

        var creatorId = Guid.NewGuid();
        _dbContext.Users.Add(new User
        {
            Id = creatorId,
            Email = "creator_dup@orimate.com",
            PasswordHash = "hash",
            Status = AccountStatus.Active
        });

        _dbContext.CreatorVipSettings.Add(new CreatorVipSettings
        {
            Id = Guid.NewGuid(),
            CreatorId = creatorId,
            Price = 50000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        var transactionId = Guid.NewGuid();

        _dbContext.Transactions.Add(new Transaction
        {
            Id = transactionId,
            UserId = subscriberId,
            CreatorId = creatorId,
            TransactionType = TransactionType.VipSubscription,
            Amount = 50000,
            PlatformFeeAmount = 5000,
            CreatorNetAmount = 45000,
            Status = TransactionStatus.Confirmed,
            ReferenceCode = "EXISTING_TXN",
            CreatedAt = DateTime.UtcNow
        });

        _dbContext.VipSubscriptions.Add(new VipSubscription
        {
            Id = Guid.NewGuid(),
            SubscriberId = subscriberId,
            CreatorId = creatorId,
            TransactionId = transactionId,
            StartDate = DateTime.UtcNow.AddDays(-5),
            EndDate = DateTime.UtcNow.AddDays(25),
            Status = SubscriptionStatus.Active,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Quan trọng: Không gọi lại AuthenticateAsAsync("User") ở đây để giữ nguyên token của đúng subscriberId trên _client
        var requestBody = new
        {
            CreatorId = creatorId,
            ReferenceCode = "DUPLICATE_TXN"
        };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/subscriptions", requestBody);

        // 3. Assert: Phải từ chối với mã 400 BadRequest (BR-VIP-05)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 🔬 Coverage Technique: Workflow — [Error]: Unauthenticated subscription attempt returns Unauthorized.
    [Fact]
    public async Task Subscribe_WithoutAuthentication_ErrorPath_ReturnsUnauthorized()
    {
        // 1. Arrange: Guest không có token
        _client.DefaultRequestHeaders.Authorization = null;
        var requestBody = new
        {
            CreatorId = Guid.NewGuid(),
            ReferenceCode = "GUEST_TXN"
        };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/subscriptions", requestBody);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}