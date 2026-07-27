using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.IntegrationTests; // Đảm bảo namespace này khớp với IntegrationTestBase
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers;

public class AuthControllerTests : IntegrationTestBase
{
    public AuthControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnSuccess_And_CreateUserInDb()
    {
        // 1. ARRANGE (Chuẩn bị dữ liệu)
        // Vì RegisterRequest là C# 9 Record, ta truyền tham số trực tiếp vào constructor
        var registerRequest = new RegisterRequest(
            "newuser@origami.com",   // Email
            "StrongPassword123!",    // Password
            "Origami Master"         // DisplayName
        );

        // 2. ACT (Hành động - Bắn API)
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        // 3. ASSERT (Kiểm tra kết quả)
        // 3.1. API phải trả về thành công (HTTP 200 OK)
        response.IsSuccessStatusCode.Should().BeTrue();

        // 3.2. Lấy User từ DB ra để kiểm tra. Include cả Profile để xem DisplayName đã lưu chưa.
        var userInDb = await _dbContext.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email == registerRequest.Email);

        userInDb.Should().NotBeNull();
        userInDb.Email.Should().Be(registerRequest.Email);

        // Mật khẩu trong DB phải được băm (hash), không được lưu plain-text
        userInDb.PasswordHash.Should().NotBeNullOrEmpty();
        userInDb.PasswordHash.Should().NotBe(registerRequest.Password);

        // (Tùy chọn) Kiểm tra xem Profile có được tạo với DisplayName đúng không
        if (userInDb.Profile != null)
        {
            userInDb.Profile.DisplayName.Should().Be(registerRequest.DisplayName);
        }
    }
}