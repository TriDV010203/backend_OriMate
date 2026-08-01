using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.IntegrationTests;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.Auth;

public class AuthWorkflowTests : IntegrationTestBase
{
    public AuthWorkflowTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CompleteOnboardingFlow_Register_VerifyEmail_ThenLogin_ShouldSucceed()
    {
        // =========================================================================
        // KỊCH BẢN 1: HAPPY PATH - LUỒNG CHUẨN ĐẸP
        // =========================================================================
        var email = "flow_onboarding@origami.com";
        var password = "StrongPassword123!";

        // BƯỚC 1: Đăng ký
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, "Flow Test"));
        registerResponse.IsSuccessStatusCode.Should().BeTrue();

        var userInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        var tokenForVerification = userInDb!.VerificationToken;

        // BƯỚC 2: Kích hoạt Email
        var verifyResponse = await _client.GetAsync($"/api/auth/verify-email?token={tokenForVerification}");
        verifyResponse.IsSuccessStatusCode.Should().BeTrue();

        // BƯỚC 3: Đăng nhập
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.IsSuccessStatusCode.Should().BeTrue("Đăng nhập thành công sau khi đã xác thực email");

        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        authData!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CompleteOnboardingFlow_SkipVerification_ShouldFailToLogin()
    {
        // =========================================================================
        // KỊCH BẢN 2: ERROR PATH - NGƯỜI DÙNG NHẢY CÓC (SKIP VERIFY)
        // =========================================================================
        var email = "flow_skip_verify@origami.com";
        var password = "StrongPassword123!";

        // BƯỚC 1: Đăng ký tài khoản (Thành công, tạo DB Status = Unverified)
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, "Impatient User"));
        registerResponse.IsSuccessStatusCode.Should().BeTrue("Đăng ký thành công");

        // BƯỚC 2: CỐ TÌNH BỎ QUA BƯỚC CLICK LINK XÁC THỰC EMAIL

        // BƯỚC 3: Bay thẳng vào gọi API Đăng nhập
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

        // THEN: Luồng phải bị bẻ gãy ở đây
        loginResponse.IsSuccessStatusCode.Should().BeFalse("Hệ thống phải chặn đứng việc đăng nhập nếu người dùng cố tình lách luật bỏ qua bước xác thực");
        loginResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PasswordResetFlow_RequestReset_ThenLoginWithOldPassword_ShouldFail()
    {
        // =========================================================================
        // KỊCH BẢN 3: ERROR PATH LUỒNG PASSWORD - CỐ TÌNH DÙNG LẠI MẬT KHẨU CŨ
        // =========================================================================
        var email = "flow_reset_trick@origami.com";
        var oldPassword = "OldPassword123!";
        var newPassword = "NewBrandPassword999!";

        // Tiền điều kiện: Setup User đã Active
        var testUser = new OrigamiPlatform.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword),
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        // BƯỚC 1: Gọi API cấp Token quên mật khẩu
        await _client.PostAsJsonAsync("/api/auth/forgot-password", new { Email = email });
        var userInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        var resetToken = userInDb!.PasswordResetToken;

        var resetRequest = new { Token = resetToken, NewPassword = newPassword, ConfirmPassword = newPassword };
        var resetResponse = await _client.PostAsJsonAsync("/api/auth/reset-password", resetRequest);
        resetResponse.IsSuccessStatusCode.Should().BeTrue("Đặt lại mật khẩu thành công");

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, oldPassword));

        loginResponse.IsSuccessStatusCode.Should().BeFalse("Luồng Reset Pass đã hoàn tất, mật khẩu cũ không được phép có hiệu lực nữa");
        loginResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest); 

        var loginNewResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, newPassword));
        loginNewResponse.IsSuccessStatusCode.Should().BeTrue("Phải dùng mật khẩu mới mới đăng nhập được");
    }
}