using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Domain.Entities;
using Xunit;

namespace OrigamiPlatform.IntegrationTests.Services.Admin_ConfigService;

public class AdminCategoriesControllerIntegrationTests : IntegrationTestBase
{
    public AdminCategoriesControllerIntegrationTests(CustomWebApplicationFactory factory) : base(factory) { }

    // 🔬 Coverage Technique: Happy Path (Verify primary success flow)
    [Fact]
    public async Task CreateCategory_AsAdmin_ReturnsSuccess_HappyPath()
    {
        await AuthenticateAsAsync("Admin");
        var req = new { name = $"Category_{Guid.NewGuid().ToString()[..6]}" };

        var response = await _client.PostAsJsonAsync("/api/admin/categories", req);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("name").GetString().Should().Be(req.name);

        _dbContext.ChangeTracker.Clear();
        var dbCategory = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Name == req.name);
        dbCategory.Should().NotBeNull();
        dbCategory!.IsActive.Should().BeTrue();
    }

    // 🔬 Coverage Technique: Error Path (Verify failure/authorization scenarios)
    [Fact]
    public async Task CreateCategory_AsNonAdmin_ReturnsForbidden_ErrorPath()
    {
        await AuthenticateAsAsync("User");
        var req = new { name = "Unauthorized Category" };

        var response = await _client.PostAsJsonAsync("/api/admin/categories", req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // 🔬 Coverage Technique: Error Path (Verify duplicate unique constraint failure)
    [Fact]
    public async Task CreateCategory_DuplicateName_ReturnsConflict_ErrorPath()
    {
        await AuthenticateAsAsync("Admin");
        var categoryName = $"DuplicateCat_{Guid.NewGuid().ToString()[..6]}";
        await _client.PostAsJsonAsync("/api/admin/categories", new { name = categoryName });

        var response = await _client.PostAsJsonAsync("/api/admin/categories", new { name = categoryName });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // 🔬 Coverage Technique: Concurrency (Two threads simultaneously attempt same operation)
    [Fact]
    public async Task CreateCategory_ConcurrentDuplicate_OnlyOneSucceeds()
    {
        await AuthenticateAsAsync("Admin");
        var categoryName = $"RaceCat_{Guid.NewGuid().ToString()[..6]}";
        var req = new { name = categoryName };

        using var client1 = _factory.CreateClient();
        using var client2 = _factory.CreateClient();

        var token = _client.DefaultRequestHeaders.Authorization?.Parameter;
        client1.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        client2.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Mô phỏng đồng thời chính xác bằng cách đồng bộ hóa thời điểm bắt đầu request qua Task.Yield / SemaphoreSlim
        var barrier = new SemaphoreSlim(0);

        async Task<HttpResponseMessage> SendConcurrentRequest(HttpClient client)
        {
            await barrier.WaitAsync();
            return await client.PostAsJsonAsync("/api/admin/categories", req);
        }

        var task1 = SendConcurrentRequest(client1);
        var task2 = SendConcurrentRequest(client2);

        // Giải phóng đồng thời cả 2 luồng vào microsecond tiếp theo
        barrier.Release(2);

        var responses = await Task.WhenAll(task1, task2);

        // Đảm bảo chỉ có ĐÚNG 1 request thành công (200 OK), request còn lại bị chặn lỗi xung đột (Conflict/BadRequest)
        responses.Count(r => r.IsSuccessStatusCode).Should().Be(1);
        responses.Count(r => !r.IsSuccessStatusCode).Should().Be(1);
    }

    // 🔬 Coverage Technique: Transaction Boundary (Verify soft-delete / database state transition)
    [Fact]
    public async Task DeleteCategory_ValidId_PerformsSoftDelete_TransactionBoundary()
    {
        await AuthenticateAsAsync("Admin");
        var category = new Category { Name = $"DelCat_{Guid.NewGuid().ToString()[..6]}", IsActive = true, IsDeleted = false };
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        var response = await _client.DeleteAsync($"/api/admin/categories/{category.Id}");

        response.EnsureSuccessStatusCode();

        _dbContext.ChangeTracker.Clear();
        var dbCategory = await _dbContext.Categories.FirstAsync(c => c.Id == category.Id);
        dbCategory.IsDeleted.Should().BeTrue();
        dbCategory.IsActive.Should().BeFalse();
    }
}