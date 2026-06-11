using MailKit.Net.Smtp;
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

    public EmailService(IConfiguration config)
    {
        _smtpHost = config["Email:SmtpHost"]!;
        _smtpPort = int.Parse(config["Email:SmtpPort"]!);
        _from = config["Email:From"]!;
        _baseUrl = config["Email:BaseUrl"]!;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string verificationToken, CancellationToken ct = default)
    {
        var link = $"https://{_baseUrl}/api/auth/verify-email?token={verificationToken}";

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_from));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Verify your Origami Platform account";
        message.Body = new TextPart("plain")
        {
            Text = $"Click the link below to verify your email address:\n\n{link}\n\nThis link expires in 24 hours."
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpHost, _smtpPort, false, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
