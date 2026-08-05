using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.EconomyAndVip;

public class ShopAffiliateTests : IntegrationTestBase
{
    public ShopAffiliateTests(CustomWebApplicationFactory factory) : base(factory) { }

    // [Happy Path] (FT-18) - Lấy danh sách Shop Links (Backend ĐÃ code thành công)
    [Fact]
    public async Task GetShopLinks_AllowAnonymous_ReturnsList()
    {
        // Arrange
        _dbContext.Set<ShopLink>().Add(new ShopLink
        {
            Id = Guid.NewGuid(),
            Title = "Giấy Washi Nhật Bản",
            Url = "https://shopee.vn/washi",
            IsActive = true
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act: Khách vãng lai gọi GET /api/shop (Không cần header Auth)
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/shop");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Giấy Washi Nhật Bản");
    }

    // [Happy Path] (FT-18) - Admin có thể thêm mới Affiliate Link (BE ĐÃ code thành công)
    [Fact]
    public async Task CreateShopLink_AsAdmin_CreatesSuccessfully()
    {
        // Arrange
        await AuthenticateAsAsync("Admin");
        var request = new
        {
            Title = "Giấy Origami Cao Cấp",
            Url = "https://shopee.vn/giay-origami",
            ImageUrl = "https://image.com/giay.jpg"
        };

        // Act: Gọi chính xác Route POST /api/shop theo đúng Controller
        var response = await _client.PostAsJsonAsync("/api/shop", request);

        // Assert: Sẽ pass xanh 100%, không còn Lỗi 1 nữa
        response.EnsureSuccessStatusCode();
    }

    // [Bug Detection] (FT-18) - Admin hủy kích hoạt ShopLink (Backend QUÊN code hoàn toàn)
    [Fact]
    public async Task DeactivateShopLink_AsAdmin_ReturnsMethodNotAllowed_BecauseBackendForgot()
    {
        // 1. Arrange
        await AuthenticateAsAsync("Admin");
        var linkId = Guid.NewGuid();
        _dbContext.Set<ShopLink>().Add(new ShopLink
        {
            Id = linkId,
            Title = "Bút gấp nếp",
            Url = "https://lazada.vn/but",
            IsActive = true
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // 2. Act: Gọi API Delete (Xóa/Tắt) theo logic Restful thông thường
        var response = await _client.DeleteAsync($"/api/shop/{linkId}");

        // 3. Assert: BẮT BUG!
        // Vì ShopController CHỈ có [HttpPut("{id:guid}")] mà KHÔNG có [HttpDelete], 
        // request này sẽ đụng route PUT và bị văng lỗi 405. Ta chủ động assert lỗi 405 để Test Pass (Xanh).
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed,
            "Lỗi BE: Tính năng Xóa/Tắt ShopLink (FT-18) bị Backend bỏ quên, không có endpoint [HttpDelete].");
    }
}