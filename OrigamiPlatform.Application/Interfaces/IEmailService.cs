namespace OrigamiPlatform.Application.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string verificationToken, CancellationToken ct = default);
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken ct = default);

    // FT-30: generic send, reused by ReengagementJob
    Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default);
}
