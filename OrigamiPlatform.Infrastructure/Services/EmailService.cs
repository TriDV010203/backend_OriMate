using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using OrigamiPlatform.Application.Interfaces;

namespace OrigamiPlatform.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _from;
    private readonly string _baseUrl;
    private readonly string _password;
    private readonly string _username;
    public EmailService(IConfiguration config)
    {
        _smtpHost = config["Email:SmtpHost"]!;
        _smtpPort = int.Parse(config["Email:SmtpPort"]!);
        _from = config["Email:From"]!;
        _baseUrl = config["Email:FrontendBaseUrl"]!;
        _username = config["Email:Username"];
        _password = config["Email:Password"];
    }

    public async Task SendVerificationEmailAsync(string toEmail, string verificationToken, CancellationToken ct = default)
    {
        var link = $"{_baseUrl}/xac-minh-email?token={verificationToken}";
        //var link = $"https://{_baseUrl}/api/auth/verify-email?token={verificationToken}";
        await SendAsync(toEmail, "Verify your Origami Platform account",
            $"Click the link below to verify your email address:\n\n{link}\n\nThis link expires in 24 hours.", ct);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken ct = default)
    {
        var link = $"{_baseUrl}/dat-lai-mat-khau?token={resetToken}";
        //var link = $"https://{_baseUrl}/reset-password?token={resetToken}";
        await SendAsync(toEmail, "Reset your Origami Platform password",
            $"Click the link below to reset your password:\n\n{link}\n\nThis link expires in 1 hour.\n\nIf you did not request a password reset, you can ignore this email.", ct);
    }

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_from));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(_username, _password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}