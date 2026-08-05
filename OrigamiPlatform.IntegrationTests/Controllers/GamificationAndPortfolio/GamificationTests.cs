//using System.Net;
//using System.Net.Http.Json;
//using FluentAssertions;
//using Microsoft.EntityFrameworkCore;
//using OrigamiPlatform.Domain.Entities;
//using Xunit;

//namespace OrigamiPlatform.IntegrationTests.GamificationAndPortfolio;

//public class GamificationTests : IntegrationTestBase
//{
//    public GamificationTests(CustomWebApplicationFactory factory) : base(factory) { }

//    // [Happy Path & Transaction Ledger] (FT-26, FT-28) - Mua Streak Freeze thành công và ghi nhận lịch sử giao dịch
//    [Fact]
//    public async Task PurchaseStreakFreeze_WithSufficientBalance_IncrementsInventoryAndDeductsHatGap()
//    {
//        // 1. Arrange: Tạo User và cấp sẵn 50 Hạt Gấp
//        var userId = await AuthenticateAsAsync("User");
//        var profile = new UserProfile
//        {
//            Id = Guid.NewGuid(),
//            UserId = userId,
//            HatGapBalance = 50,
//            StreakFreezes = 0
//        };
//        _dbContext.UserProfiles.Add(profile);
//        await _dbContext.SaveChangesAsync();

//        // 2. Act: Gọi API mua Streak Freeze
//        // Chú ý: Route POST thường được dùng cho các action tạo thay đổi trong hệ thống
//        var response = await _client.PostAsync("/api/gamification/streak-freezes", null);

//        // Cơ chế Fallback tìm đúng Route của Backend
//        if (response.StatusCode == HttpStatusCode.NotFound)
//            response = await _client.PostAsync("/api/gamification/streak-freeze", null);
//        if (response.StatusCode == HttpStatusCode.NotFound)
//            response = await _client.PostAsync("/api/gamification/purchases/streak-freeze", null);

//        response.EnsureSuccessStatusCode();

//        // 3. Assert: Kiểm tra số dư và kho đồ
//        _dbContext.ChangeTracker.Clear();
//        var updatedProfile = await _dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

//        updatedProfile.Should().NotBeNull();
//        updatedProfile!.StreakFreezes.Should().Be(1, "Kho đồ phải tăng lên 1 Streak Freeze");
//        updatedProfile.HatGapBalance.Should().BeLessThan(50, "Số dư Hạt Gấp phải bị trừ đi tương ứng với giá vật phẩm");

//        // Assert Ledger: Phải có 1 transaction ghi lại biến động (FT-28)
//        var transaction = await _dbContext.HatGapTransactions.FirstOrDefaultAsync(t => t.UserId == userId);
//        transaction.Should().NotBeNull("Hệ thống phải lưu lại lịch sử giao dịch Hạt Gấp (Ledger)");
//    }

//    // [BVA / Error Path] (FT-26) - Cố tình mua quá giới hạn 2 Streak Freezes (BR-SEEDS-02)
//    [Fact]
//    public async Task PurchaseStreakFreeze_WhenInventoryIsFull_ReturnsBadRequest()
//    {
//        // 1. Arrange: User có dư Hạt Gấp nhưng kho đồ ĐÃ FULL 2 Streak Freezes
//        var userId = await AuthenticateAsAsync("User");
//        var profile = new UserProfile
//        {
//            Id = Guid.NewGuid(),
//            UserId = userId,
//            HatGapBalance = 100,
//            StreakFreezes = 2 // Đã đạt mốc giới hạn tối đa theo BR-SEEDS-02[cite: 1]
//        };
//        _dbContext.UserProfiles.Add(profile);
//        await _dbContext.SaveChangesAsync();

//        // 2. Act
//        var response = await _client.PostAsync("/api/gamification/streak-freezes", null);
//        if (response.StatusCode == HttpStatusCode.NotFound)
//            response = await _client.PostAsync("/api/gamification/streak-freeze", null);

//        // 3. Assert
//        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Lỗi BE: Không chặn hành vi mua khi đã đạt tối đa 2 Streak Freezes (BR-SEEDS-02)");
//    }

//    // [Error Path] (FT-28) - Cố tình mua khi không đủ Hạt Gấp
//    [Fact]
//    public async Task PurchaseStreakFreeze_WithInsufficientBalance_ReturnsBadRequest()
//    {
//        // 1. Arrange: User hoàn toàn không có Hạt Gấp (Balance = 0)
//        var userId = await AuthenticateAsAsync("User");
//        var profile = new UserProfile
//        {
//            Id = Guid.NewGuid(),
//            UserId = userId,
//            HatGapBalance = 0,
//            StreakFreezes = 0
//        };
//        _dbContext.UserProfiles.Add(profile);
//        await _dbContext.SaveChangesAsync();

//        // 2. Act
//        var response = await _client.PostAsync("/api/gamification/streak-freezes", null);
//        if (response.StatusCode == HttpStatusCode.NotFound)
//            response = await _client.PostAsync("/api/gamification/streak-freeze", null);

//        // 3. Assert
//        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Hệ thống phải trả về Bad Request nếu số dư Hạt Gấp không đủ để thanh toán");
//    }
//}