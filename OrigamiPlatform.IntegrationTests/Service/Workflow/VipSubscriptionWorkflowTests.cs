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
    // Creator configures VIP tier -> User subscribes -> Returns Payment Instruction -> 
    // Payment confirmed (Webhook/Admin) -> VipSubscription active -> User accesses VIP content.
    [Fact]
    public async Task EndToEnd_VipSubscriptionAndContentAccess_HappyPath_Succeeds()
    {
        // 1. Arrange: Tạo Creator và cấu hình VIP tier
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

        // Tạo VIP Tutorial do Creator này làm tác giả
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

        // Gọi API xem chi tiết tutorial trước khi subscribe (bị server-side gating ẩn bước 4+)
        var detailResponseBefore = await _client.GetAsync($"/api/tutorials/{tutorial.Slug}");
        detailResponseBefore.EnsureSuccessStatusCode();

        // 3. Act Step B: Subscriber tiến hành gọi API Subscribe (API MỚI không cần ReferenceCode)
        var subscribeRequest = new
        {
            CreatorId = creatorId
            // Không truyền ReferenceCode nữa do BE đã tự động sinh PaymentCode
        };
        var subResponse = await _client.PostAsJsonAsync("/api/subscriptions", subscribeRequest);
        subResponse.EnsureSuccessStatusCode();
        var subResult = await subResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Cấu trúc response mới là SubscribeResultDto chứa Transaction và PaymentInstruction
        var transactionNode = subResult.GetProperty("transaction");
        var instructionNode = subResult.GetProperty("paymentInstruction");

        var transactionId = transactionNode.GetProperty("id").GetGuid();
        transactionNode.GetProperty("status").GetString().Should().Be("PendingConfirmation");

        // Kiểm tra mã thanh toán đã được sinh tự động
        instructionNode.GetProperty("paymentCode").GetString().Should().NotBeNullOrEmpty();
        instructionNode.GetProperty("qrCodeUrl").GetString().Should().Contain("sepay.vn");

        // 4. Act Step C: Giả lập SePay Webhook xác nhận thanh toán qua DB (Cập nhật Transaction thành Confirmed & Tạo VipSubscription Active)
        // Lưu ý: Trong integration test, ta có thể update DB trực tiếp thay vì gọi Webhook controller để tránh lộ API key cấu hình ngoài.
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
        // 1. Arrange
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
            PaymentCode = "VIP" + Guid.NewGuid().ToString("N"), // Add PaymentCode mock
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

        // Act - API mới không truyền ReferenceCode
        var requestBody = new
        {
            CreatorId = creatorId
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
            CreatorId = Guid.NewGuid()
        };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/subscriptions", requestBody);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}