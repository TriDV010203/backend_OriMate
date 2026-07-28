using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.IntegrationTests;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Controllers.Auth;

public class PasswordAndSecurityControllerTests : IntegrationTestBase
{
    public PasswordAndSecurityControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }    

    [Fact]
    public async Task ForgotPassword_WithValidActiveEmail_ShouldSetResetToken()
    {
        // GIVEN: User đang Active
        var email = "forgot_pass@origami.com";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "SomeHash",
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        // WHEN
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { Email = email });

        // THEN
        response.IsSuccessStatusCode.Should().BeTrue("API Forgot Password phải trả về 200 OK");

        var userInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        userInDb!.PasswordResetToken.Should().NotBeNullOrEmpty("Reset password token phải được tạo và lưu DB");
    }


    [Fact]
    public async Task ForgotPassword_WithUnverifiedUser_ShouldReturnBadRequest()
    {
        // GIVEN: User tồn tại nhưng chưa Verify
        var email = "unverified_forgot@origami.com";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "SomeHash",
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Unverified, // Chưa xác thực
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        // WHEN
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { Email = email });

        // THEN
        response.IsSuccessStatusCode.Should().BeFalse("Hệ thống không cho phép reset mật khẩu khi chưa xác thực email");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ShouldUpdatePassword()
    {
        // GIVEN
        var email = "reset_pass@origami.com";
        var resetToken = "VALID_RESET_TOKEN_999";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!"),
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Active,
            PasswordResetToken = resetToken,
            TokenExpiry = DateTime.UtcNow.AddHours(1), // Còn hạn
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var newPassword = "NewSecurePassword123!";
        var requestPayload = new { Token = resetToken, NewPassword = newPassword };

        // WHEN
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", requestPayload);

        // THEN
        response.IsSuccessStatusCode.Should().BeTrue("API Reset Password phải thành công khi Token hợp lệ");

        var userInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        userInDb!.PasswordResetToken.Should().BeNullOrEmpty("Reset token phải bị xóa sau khi xài xong");
        BCrypt.Net.BCrypt.Verify(newPassword, userInDb.PasswordHash).Should().BeTrue("Mật khẩu trong DB phải được đổi sang mật khẩu mới");
    }

    [Fact]
    public async Task ResetPassword_WithWeakPassword_ShouldReturnBadRequest()
    {
        // GIVEN (Mô phỏng NAC-03: Mật khẩu yếu)
        var email = "nac03_test@origami.com";
        var resetToken = "VALID_TOKEN_NAC03";
        var oldHash = BCrypt.Net.BCrypt.HashPassword("StrongOldPass123!");
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = oldHash,
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Active,
            PasswordResetToken = resetToken,
            TokenExpiry = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        // WHEN: Truyền mật khẩu chỉ có 3 ký tự
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new { Token = resetToken, NewPassword = "123" });

        // THEN
        response.IsSuccessStatusCode.Should().BeFalse("Hệ thống phải từ chối mật khẩu không đủ độ mạnh");

        var userInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        userInDb!.PasswordHash.Should().Be(oldHash, "Mật khẩu cũ không được phép bị ghi đè");
    }

    [Fact]
    public async Task ChangePassword_WithValidCredentials_ShouldUpdatePassword()
    {
        // GIVEN: Tạo User & Login để lấy Token
        var email = "change_pass@origami.com";
        var oldPassword = "OldPassword123!";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword),
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = oldPassword });
        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var newPassword = "BrandNewPassword123!";
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password");
        httpRequest.Content = JsonContent.Create(new { CurrentPassword = oldPassword, NewPassword = newPassword });
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authData!.Token);

        // WHEN
        var response = await _client.SendAsync(httpRequest);

        // THEN
        response.IsSuccessStatusCode.Should().BeTrue("API Change Password phải trả về 200 OK");

        var userInDb = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        BCrypt.Net.BCrypt.Verify(newPassword, userInDb!.PasswordHash).Should().BeTrue("Mật khẩu mới phải được cập nhật vào DB");
    }

    [Fact]
    public async Task ChangePassword_WithWrongOldPassword_ShouldReturnBadRequest()
    {
        // GIVEN
        var email = "wrong_old_pass@origami.com";
        var realOldPassword = "CorrectPassword123!";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(realOldPassword),
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = realOldPassword });
        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password");
        httpRequest.Content = JsonContent.Create(new { CurrentPassword = "WrongOldPassword999!", NewPassword = "NewPassword123!" });
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authData!.Token);

        // WHEN
        var response = await _client.SendAsync(httpRequest);

        // THEN
        response.IsSuccessStatusCode.Should().BeFalse("Mật khẩu cũ bị sai thì không được đổi");
    }

    [Fact]
    public async Task ChangePassword_WithNewPasswordSameAsOld_ShouldReturnBadRequest()
    {
        // GIVEN
        var email = "same_pass@origami.com";
        var password = "SamePassword123!";
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Status = OrigamiPlatform.Domain.Enums.AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password");
        httpRequest.Content = JsonContent.Create(new { CurrentPassword = password, NewPassword = password }); // Mật khẩu mới trùng cũ
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authData!.Token);

        // WHEN
        var response = await _client.SendAsync(httpRequest);

        // THEN
        response.IsSuccessStatusCode.Should().BeFalse("Hệ thống không được phép cho đổi mật khẩu mới trùng mật khẩu cũ");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}