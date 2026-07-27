using Microsoft.Extensions.DependencyInjection;
using OrigamiPlatform.Infrastructure.Persistence;
using Xunit;

namespace OrigamiPlatform.IntegrationTests;

// IClassFixture giúp dùng chung 1 instance CustomWebApplicationFactory cho tất cả các test trong cùng 1 class
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory _factory;
    protected readonly HttpClient _client;
    protected readonly AppDbContext _dbContext;

    public IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        _factory = factory;

        // Tạo HttpClient giả lập gọi HTTP request tới API
        _client = factory.CreateClient();

        // Lấy AppDbContext để kiểm tra trực tiếp dữ liệu lưu trong DB sau khi gọi API
        var scope = factory.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }
}