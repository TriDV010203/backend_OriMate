using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrigamiPlatform.Application.Commands.Auth;
using OrigamiPlatform.Application.DTOs.Auth;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegisterUserHandler _register;
    private readonly LoginHandler _login;
    private readonly VerifyEmailHandler _verifyEmail;
    private readonly ResendVerificationHandler _resendVerification;
    private readonly ForgotPasswordHandler _forgotPassword;
    private readonly ResetPasswordHandler _resetPassword;
    private readonly RefreshTokenHandler _refreshToken;
    private readonly ChangePasswordHandler _changePassword;
    private readonly LogoutHandler _logout;

    public AuthController(
        RegisterUserHandler register,
        LoginHandler login,
        VerifyEmailHandler verifyEmail,
        ResendVerificationHandler resendVerification,
        ForgotPasswordHandler forgotPassword,
        ResetPasswordHandler resetPassword,
        RefreshTokenHandler refreshToken,
        ChangePasswordHandler changePassword,
        LogoutHandler logout)
        => (_register, _login, _verifyEmail, _resendVerification, _forgotPassword, _resetPassword,
            _refreshToken, _changePassword, _logout)
            = (register, login, verifyEmail, resendVerification, forgotPassword, resetPassword,
               refreshToken, changePassword, logout);

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await _register.HandleAsync(
            new RegisterUserCommand(request.Email, request.Password, request.DisplayName), ct);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await _login.HandleAsync(
            new LoginCommand(request.Email, request.Password), ct);
        return Ok(result);
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token, CancellationToken ct)
    {
        var result = await _verifyEmail.HandleAsync(new VerifyEmailCommand(token), ct);
        return Ok(result);
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(ResendVerificationRequest request, CancellationToken ct)
    {
        var result = await _resendVerification.HandleAsync(
            new ResendVerificationCommand(request.Email), ct);
        return Ok(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        var result = await _forgotPassword.HandleAsync(
            new ForgotPasswordCommand(request.Email), ct);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await _resetPassword.HandleAsync(
            new ResetPasswordCommand(request.Token, request.NewPassword, request.ConfirmPassword), ct);
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _refreshToken.HandleAsync(new RefreshTokenCommand(request.RefreshToken), ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _changePassword.HandleAsync(
            new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword, request.ConfirmPassword), ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _logout.HandleAsync(new LogoutCommand(userId), ct);
        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new ForbiddenException("User identity not found in token.");
        return Guid.Parse(sub);
    }
}
