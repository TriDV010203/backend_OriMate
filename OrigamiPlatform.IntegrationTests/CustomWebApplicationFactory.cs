using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Infrastructure.Persistence;
using Xunit;

namespace OrigamiPlatform.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Sử dụng LocalDB có sẵn trong mọi máy cài Visual Studio / .NET (hoặc thay bằng chuỗi kết nối SQL Server của bạn)
    private const string ConnectionString = "Server=(localdb)\\mssqllocaldb;Database=OrigamiPlatform_IntegrationTest;Trusted_Connection=True;MultipleActiveResultSets=true";

    public Task InitializeAsync() => Task.CompletedTask;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // 1. Xóa cấu hình AppDbContext cũ
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));

            // 2. Trỏ AppDbContext về SQL Server cục bộ trên máy
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(ConnectionString));

            // 3. Mock các dịch vụ ngoài (Email, File Storage)
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

            // 4. Tự động tạo Database và chạy Migration của SQL Server
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Xóa sạch DB cũ của test (nếu có) và tạo mới để đảm bảo môi trường sạch sẽ
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
        });
    }

    public new Task DisposeAsync() => Task.CompletedTask;
}