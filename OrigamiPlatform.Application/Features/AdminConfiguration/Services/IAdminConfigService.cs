using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;

namespace OrigamiPlatform.Application.Features.AdminConfiguration.Services;

public interface IAdminConfigService
{
    // Categories
    Task<List<CategoryResponse>> GetCategoriesAsync(CancellationToken ct = default);
    Task<CategoryResponse> CreateCategoryAsync(Guid actorId, CreateCategoryRequest req, CancellationToken ct = default);
    Task<CategoryResponse> UpdateCategoryAsync(Guid actorId, int id, UpdateCategoryRequest req, CancellationToken ct = default);

    // Blocked words
    Task<List<BlockedWordResponse>> GetBlockedWordsAsync(CancellationToken ct = default);
    Task<BlockedWordResponse> AddBlockedWordAsync(Guid actorId, CreateBlockedWordRequest req, CancellationToken ct = default);
    Task RemoveBlockedWordAsync(Guid actorId, int id, CancellationToken ct = default);

    // User management
    Task<PagedResult<AdminUserResponse>> GetUsersAsync(string? keyword, string? status, string? role, int page, int pageSize, CancellationToken ct = default);
    Task AssignRoleAsync(Guid actorId, Guid userId, AssignRoleRequest req, CancellationToken ct = default);
    Task RemoveRoleAsync(Guid actorId, Guid userId, RemoveRoleRequest req, CancellationToken ct = default);
    Task SuspendUserAsync(Guid actorId, Guid userId, SuspendUserRequest req, CancellationToken ct = default);
    Task ActivateUserAsync(Guid actorId, Guid userId, CancellationToken ct = default);
}
