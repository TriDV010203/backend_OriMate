using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
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
}