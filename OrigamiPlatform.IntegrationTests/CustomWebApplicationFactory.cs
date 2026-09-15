using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Infrastructure.Persistence;

namespace OrigamiPlatform.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string DatabaseName = "OriMateL2IntegrationTestDb";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=(localdb)\\MSSQLLocalDB;Database=unused;",
                ["Jwt:Key"] = "origami-platform-integration-test-secret-key",
                ["Jwt:Issuer"] = "OrigamiPlatform",
                ["Jwt:Audience"] = "OrigamiPlatform",
                ["Jwt:ExpiryMinutes"] = "60",
                ["Jwt:RefreshTokenExpiryDays"] = "30",
                ["SePay:WebhookApiKey"] = "Orimate2026"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(DatabaseName));

            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, NoOpEmailService>();
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    private sealed class NoOpEmailService : IEmailService
    {
        public Task SendVerificationEmailAsync(string toEmail, string verificationToken, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
