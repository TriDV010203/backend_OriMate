namespace OrigamiPlatform.Application.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string verificationToken, CancellationToken ct = default);
}
