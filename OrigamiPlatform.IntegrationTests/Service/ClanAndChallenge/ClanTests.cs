using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.ClanAndChallenge;

public class ClanTests : IntegrationTestBase
{
    public ClanTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path: Verify the primary success flow — User creates a Clan and becomes Owner (FT-22 / BR-CLAN-01).
    [Fact]
    public async Task CreateClan_ValidName_SucceedsAndAssignsOwner()
    {
        // 1. Arrange
        var userId = await AuthenticateAsAsync("User");
        var request = new { Name = "Ha Noi Paper Cranes" };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/clans", request);

        // 3. Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("name").GetString().Should().Be("Ha Noi Paper Cranes");

        _dbContext.ChangeTracker.Clear();
        var clanInDb = await _dbContext.Clans.FirstOrDefaultAsync(c => c.Name == request.Name);
        clanInDb.Should().NotBeNull();
        clanInDb!.OwnerId.Should().Be(userId);

        var memberInDb = await _dbContext.ClanMembers.FirstOrDefaultAsync(m => m.ClanId == clanInDb.Id && m.UserId == userId);
        memberInDb.Should().NotBeNull();
    }

    // 🔬 Coverage Technique: Error Path: Verify user already in a Clan cannot create or join a second Clan (BR-CLAN-01).
    [Fact]
    public async Task CreateClan_WhenAlreadyInClan_ReturnsBadRequest()
    {
        // 1. Arrange: User đã là thành viên của 1 Clan
        var userId = await AuthenticateAsAsync("User");
        var clanId = Guid.NewGuid();

        _dbContext.Clans.Add(new Clan
        {
            Id = clanId,
            Name = "Existing Clan",
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        });
        _dbContext.ClanMembers.Add(new ClanMember
        {
            Id = Guid.NewGuid(),
            ClanId = clanId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var request = new { Name = "Second Clan Attempt" };

        // 2. Act
        var response = await _client.PostAsJsonAsync("/api/clans", request);

        // 3. Assert: Phải trả về 400 BadRequest (BR-CLAN-01)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // 🔬 Coverage Technique: Happy Path: Verify Clan owner can invite another user successfully (BR-CLAN-02).
    [Fact]
    public async Task InviteMember_ByClanOwner_Succeeds()
    {
        // 1. Arrange
        var ownerId = await AuthenticateAsAsync("User");
        var clanId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();

        _dbContext.Users.Add(new User
        {
            Id = inviteeId,
            Email = "invitee@orimate.com",
            PasswordHash = "hashed",
            Status = AccountStatus.Active
        });

        _dbContext.Clans.Add(new Clan
        {
            Id = clanId,
            Name = "Owner Clan",
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        });
        _dbContext.ClanMembers.Add(new ClanMember
        {
            Id = Guid.NewGuid(),
            ClanId = clanId,
            UserId = ownerId,
            JoinedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var request = new { InviteeUserId = inviteeId };

        // 2. Act
        var response = await _client.PostAsJsonAsync($"/api/clans/{clanId}/invites", request);

        // 3. Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var inviteInDb = await _dbContext.ClanInvites.FirstOrDefaultAsync(i => i.ClanId == clanId && i.UserId == inviteeId);
        inviteInDb.Should().NotBeNull();
        inviteInDb!.Status.Should().Be(ClanInviteStatus.Pending);
    }

    // 🔬 Coverage Technique: Error Path: Verify non-owner cannot invite members to the Clan.
    [Fact]
    public async Task InviteMember_ByNonOwner_ReturnsForbidden()
    {
        // 1. Arrange: Tạo User chủ Clan hợp lệ trước để thỏa mãn khóa ngoại OwnerId
        var ownerId = Guid.NewGuid();
        _dbContext.Users.Add(new User
        {
            Id = ownerId,
            Email = "clanowner@orimate.com",
            PasswordHash = "hash",
            Status = AccountStatus.Active
        });

        var nonOwnerId = await AuthenticateAsAsync("User");
        var clanId = Guid.NewGuid();

        _dbContext.Clans.Add(new Clan
        {
            Id = clanId,
            Name = "Other Clan",
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var request = new { InviteeUserId = Guid.NewGuid() };

        // 2. Act
        var response = await _client.PostAsJsonAsync($"/api/clans/{clanId}/invites", request);

        // 3. Assert: Phải bị chặn với mã 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}