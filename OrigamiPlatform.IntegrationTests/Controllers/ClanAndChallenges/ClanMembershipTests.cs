using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.ClanAndChallenges;

public class ClanMembershipTests : IntegrationTestBase
{
    public ClanMembershipTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task CreateClan_ValidData_CreatesSuccessfullyAndAssignsOwner()
    {
        var userId = await AuthenticateAsAsync("User");
        var request = new { Name = "Hội Hạc Giấy Hà Nội", Description = "Nơi hội tụ những người yêu thích gấp hạc" };

        // ĐÃ SỬA: Dùng đúng URL POST /api/clans theo cấu hình [Route("api/clans")] của BE
        var response = await _client.PostAsJsonAsync("/api/clans", request);
        response.EnsureSuccessStatusCode();

        _dbContext.ChangeTracker.Clear();
        var clanMember = await _dbContext.ClanMembers.FirstOrDefaultAsync(cm => cm.UserId == userId);
        var clan = await _dbContext.Clans.FirstOrDefaultAsync(c => c.Name == request.Name);

        clan.Should().NotBeNull("Clan phải được lưu vào Database");
        clanMember.Should().NotBeNull("User tạo Clan phải tự động trở thành ClanMember");
        clan!.OwnerId.Should().Be(userId);
    }

    [Fact]
    public async Task CreateClan_WhenUserAlreadyInAClan_ReturnsBadRequest()
    {
        var userId = await AuthenticateAsAsync("User");
        var existingClan = new Clan { Id = Guid.NewGuid(), Name = "Clan Đời Đầu", OwnerId = userId };
        _dbContext.Clans.Add(existingClan);

        _dbContext.ClanMembers.Add(new ClanMember { Id = Guid.NewGuid(), ClanId = existingClan.Id, UserId = userId });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var request = new { Name = "Clan Thứ Hai", Description = "Tham lam muốn có 2 Clan" };
        var response = await _client.PostAsJsonAsync("/api/clans", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Hệ thống vi phạm BR-CLAN-01[cite: 1]");
    }

    [Fact]
    public async Task LeaveClan_AsOwnerWithoutTransferring_ReturnsBadRequest()
    {
        var ownerId = await AuthenticateAsAsync("User");
        var clanId = Guid.NewGuid();
        _dbContext.Clans.Add(new Clan { Id = clanId, Name = "Solo Clan", OwnerId = ownerId });

        _dbContext.ClanMembers.Add(new ClanMember { Id = Guid.NewGuid(), ClanId = clanId, UserId = ownerId });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // ĐÃ SỬA TẬN GỐC: Gọi chính xác HTTP DELETE vào endpoint /api/clans/{clanId}/members/me[cite: 2]
        var response = await _client.DeleteAsync($"/api/clans/{clanId}/members/me");

        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound, "Lỗi Routing: Vẫn không tìm thấy Endpoint Leave Clan.");

        // Theo BR-CLAN-03, Owner không được phép rời Clan nếu chưa chuyển quyền[cite: 1]
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Hệ thống vi phạm BR-CLAN-03 vì cho phép Owner rời đi khi chưa chuyển quyền.");

        _dbContext.ChangeTracker.Clear();
        var stillMember = await _dbContext.ClanMembers.AnyAsync(cm => cm.UserId == ownerId);
        stillMember.Should().BeTrue("Dữ liệu Member không được phép bị xóa khi thao tác Leave bị chặn!");
    }

    [Fact]
    public async Task TransferOwnership_ThenLeaveClan_ShouldSucceed() // [Happy Path / State Transition]
    {
        // 1. Arrange: Tạo Clan, có Owner (User1) và 1 Member bình thường (User2)
        var ownerId = await AuthenticateAsAsync("User");
        var clanId = Guid.NewGuid();
        var clan = new Clan { Id = clanId, Name = "Chuyển Quyền Clan", OwnerId = ownerId };
        _dbContext.Clans.Add(clan);

        var memberId = Guid.NewGuid();
        _dbContext.Users.Add(new User { Id = memberId, Email = "member@origami.com", PasswordHash = "Hash", Status = AccountStatus.Active });

        _dbContext.ClanMembers.Add(new ClanMember { Id = Guid.NewGuid(), ClanId = clanId, UserId = ownerId });
        _dbContext.ClanMembers.Add(new ClanMember { Id = Guid.NewGuid(), ClanId = clanId, UserId = memberId });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // 2. Act 1: Owner gọi API chuyển quyền cho Member
        var transferRequest = new { NewOwnerId = memberId };
        // Giả định URL API transfer là /api/clans/{clanId}/transfer-ownership (Điều chỉnh nếu BE dùng URL khác)
        var transferResponse = await _client.PutAsJsonAsync($"/api/clans/{clanId}/transfer-ownership", transferRequest);
        transferResponse.EnsureSuccessStatusCode();

        // 3. Act 2: Owner (cũ) gọi API rời Clan
        var leaveResponse = await _client.DeleteAsync($"/api/clans/{clanId}/members/me");

        // 4. Assert
        leaveResponse.IsSuccessStatusCode.Should().BeTrue("Chủ Clan sau khi chuyển quyền thì phải được phép rời Clan (BR-CLAN-03)");

        _dbContext.ChangeTracker.Clear();
        var updatedClan = await _dbContext.Clans.FindAsync(clanId);
        updatedClan!.OwnerId.Should().Be(memberId, "Quyền Owner phải được chuyển sang user mới");

        var isStillMember = await _dbContext.ClanMembers.AnyAsync(cm => cm.UserId == ownerId);
        isStillMember.Should().BeFalse("Dữ liệu Member của Owner cũ phải bị xóa khỏi Clan");
    }

    [Fact]
    public async Task AcceptClanInvite_After48Hours_ReturnsBadRequest() // [Time Boundary / BR-CLAN-02]
    {
        // 1. Arrange: Tạo 1 Invite đã quá hạn
        var inviteeId = await AuthenticateAsAsync("User");
        var clanId = Guid.NewGuid();
        _dbContext.Clans.Add(new Clan { Id = clanId, Name = "Late Clan", OwnerId = Guid.NewGuid() });

        var inviteId = Guid.NewGuid();
        var expiredInvite = new ClanInvite
        {
            Id = inviteId,
            ClanId = clanId,
            UserId = inviteeId, // ĐÃ SỬA: Đổi từ InviteeId sang UserId cho khớp Domain Entity của BE
            Status = ClanInviteStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddHours(-49),
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };
        _dbContext.ClanInvites.Add(expiredInvite);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // 2. Act: Cố tình Accept invite đó
        var response = await _client.PostAsync($"/api/clans/invites/{inviteId}/accept", null);

        // 3. Assert
        response.IsSuccessStatusCode.Should().BeFalse("Hệ thống không được cho phép chấp nhận lời mời đã quá 48 giờ (BR-CLAN-02)");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Gone);
    }

    [Fact]
    public async Task AcceptInvite_AlreadyAccepted_ShouldReturnBadRequest() // [Idempotency]
    {
        // 1. Arrange: Giả lập lời mời ĐÃ ĐƯỢC CHẤP NHẬN trước đó
        var inviteeId = await AuthenticateAsAsync("User");
        var clanId = Guid.NewGuid();
        _dbContext.Clans.Add(new Clan { Id = clanId, Name = "Idempotency Clan", OwnerId = Guid.NewGuid() });

        var inviteId = Guid.NewGuid();
        _dbContext.ClanInvites.Add(new ClanInvite
        {
            Id = inviteId,
            ClanId = clanId,
            UserId = inviteeId, // Dùng đúng tên field như đã sửa ở trên
            Status = ClanInviteStatus.Accepted, // Trạng thái đã chấp nhận
            CreatedAt = DateTime.UtcNow.AddHours(-10),
            ExpiresAt = DateTime.UtcNow.AddHours(38)
        });

        // Mô phỏng việc user đã được add vào Member table
        _dbContext.ClanMembers.Add(new ClanMember { Id = Guid.NewGuid(), ClanId = clanId, UserId = inviteeId });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // 2. Act: Cố tình gọi API Accept một lần nữa (Ví dụ do lỗi giật lag UI)
        var response = await _client.PostAsync($"/api/clans/invites/{inviteId}/accept", null);

        // 3. Assert
        response.IsSuccessStatusCode.Should().BeFalse("Hệ thống phải có tính Idempotency, từ chối lời mời đã được chấp nhận");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);

        var memberCount = await _dbContext.ClanMembers.CountAsync(cm => cm.UserId == inviteeId && cm.ClanId == clanId);
        memberCount.Should().Be(1, "Không được phép tạo ra duplicate ClanMember record");
    }
}