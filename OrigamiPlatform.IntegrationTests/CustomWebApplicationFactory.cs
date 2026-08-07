using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Infrastructure.Persistence;

namespace OrigamiPlatform.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"OrigamiPlatform_Test_{Guid.NewGuid()}";

    public const string SePayTestApiKey = "test-sepay-webhook-key";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SePay:WebhookApiKey"] = SePayTestApiKey,
                ["BankAccount:AccountNumber"] = "0123456789",
                ["BankAccount:BankName"] = "TestBank",
                ["BankAccount:BankBin"] = "970422",
                ["BankAccount:AccountHolderName"] = "ORIMATE TEST"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer($"Server=(localdb)\\mssqllocaldb;Database={_databaseName};Trusted_Connection=True;MultipleActiveResultSets=true"));

            services.RemoveAll(typeof(IEmailService));
            var emailServiceMock = new Mock<IEmailService>();
            emailServiceMock
                .Setup(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            services.AddSingleton(emailServiceMock.Object);

            services.RemoveAll(typeof(IFileStorageService));
            var fileStorageMock = new Mock<IFileStorageService>();
            fileStorageMock
                .Setup(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://fake-cloudinary.com/test-image.jpg");
            services.AddSingleton(fileStorageMock.Object);

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.Database.EnsureCreated();
        });
    }

    // SỬA LỖI 2: Override đúng chuẩn của WebApplicationFactory và gọi base.DisposeAsync()
    public override async ValueTask DisposeAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync(); // Nên dùng Async cho Db

        await base.DisposeAsync();
    }
}