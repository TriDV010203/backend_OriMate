using Microsoft.Extensions.Logging;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Infrastructure.Services;

public class DevEmailService : IEmailService
{
    private readonly ILogger<DevEmailService> _logger;

    public DevEmailService(ILogger<DevEmailService> logger) => _logger = logger;

    public Task SendVerificationEmailAsync(string toEmail, string verificationToken, CancellationToken ct = default)
    {
        _logger.LogWarning("[DEV] Verification email suppressed. Token for {Email}: {Token}", toEmail, verificationToken);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken ct = default)
    {
        _logger.LogWarning("[DEV] Password reset email suppressed. Token for {Email}: {Token}", toEmail, resetToken);
        return Task.CompletedTask;
    }
}
