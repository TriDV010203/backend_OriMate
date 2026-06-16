using System.Text.Json;
using OrigamiPlatform.Application.DTOs.Common;
using OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;
using OrigamiPlatform.Application.Features.AdminConfiguration.Validators;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Enums;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Features.AdminConfiguration.Services;

public class AdminConfigService : IAdminConfigService
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly IBlockedWordRepository _blockedWordRepo;
    private readonly IBlockedWordService _blockedWordService;
    private readonly IAuditLogRepository _auditLog;
    private readonly IUserRepository _userRepo;
    private readonly INotificationService _notifications;

    public AdminConfigService(
        ICategoryRepository categoryRepo,
        IBlockedWordRepository blockedWordRepo,
        IBlockedWordService blockedWordService,
        IAuditLogRepository auditLog,
        IUserRepository userRepo,
        INotificationService notifications)
        => (_categoryRepo, _blockedWordRepo, _blockedWordService, _auditLog, _userRepo, _notifications)
            = (categoryRepo, blockedWordRepo, blockedWordService, auditLog, userRepo, notifications);

    // ── CATEGORIES ─────────────────────────────────────────────────────

    public async Task<List<CategoryResponse>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await _categoryRepo.GetAllAsync(ct);
        return categories
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CategoryResponse(c.Id, c.Name, c.IsActive, c.CreatedAt))
            .ToList();
    }

    public async Task<CategoryResponse> CreateCategoryAsync(Guid actorId, CreateCategoryRequest req, CancellationToken ct = default)
    {
        CreateCategoryRequestValidator.Validate(req.Name);

        if (await _categoryRepo.ExistsByNameAsync(req.Name, null, ct))
            throw new ConflictException("Category name already exists.");

        var category = new Category
        {
            Name = req.Name.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _categoryRepo.AddAsync(category, ct);

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = "CreateCategory",
            EntityType = "Category",
            EntityId = created.Id.ToString(),
            OldValue = null,
            NewValue = created.Name,
            CreatedAt = DateTime.UtcNow
        }, ct);

        return new CategoryResponse(created.Id, created.Name, created.IsActive, created.CreatedAt);
    }

    public async Task<CategoryResponse> UpdateCategoryAsync(Guid actorId, int id, UpdateCategoryRequest req, CancellationToken ct = default)
    {
        UpdateCategoryRequestValidator.Validate(req.Name);

        var category = await _categoryRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Category {id} not found.");

        if (req.Name is not null && await _categoryRepo.ExistsByNameAsync(req.Name, id, ct))
            throw new ConflictException("Category name already exists.");

        var oldState = JsonSerializer.Serialize(new { category.Name, category.IsActive });

        if (req.Name is not null) category.Name = req.Name.Trim();
        if (req.IsActive.HasValue) category.IsActive = req.IsActive.Value;
        category.UpdatedAt = DateTime.UtcNow;

        await _categoryRepo.UpdateAsync(category, ct);

        var newState = JsonSerializer.Serialize(new { category.Name, category.IsActive });

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = "UpdateCategory",
            EntityType = "Category",
            EntityId = id.ToString(),
            OldValue = oldState,
            NewValue = newState,
            CreatedAt = DateTime.UtcNow
        }, ct);

        return new CategoryResponse(category.Id, category.Name, category.IsActive, category.CreatedAt);
    }

    // ── BLOCKED WORDS ───────────────────────────────────────────────────

    public async Task<List<BlockedWordResponse>> GetBlockedWordsAsync(CancellationToken ct = default)
    {
        var words = await _blockedWordRepo.GetAllAsync(ct);
        return words
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new BlockedWordResponse(w.Id, w.Word, w.CreatedAt))
            .ToList();
    }

    public async Task<BlockedWordResponse> AddBlockedWordAsync(Guid actorId, CreateBlockedWordRequest req, CancellationToken ct = default)
    {
        CreateBlockedWordRequestValidator.Validate(req.Word);

        var normalized = req.Word.Trim().ToLower();

        if (await _blockedWordRepo.ExistsByWordAsync(normalized, ct))
            throw new ConflictException("Word already in blocked list.");

        var blockedWord = new BlockedWord
        {
            Word = normalized,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _blockedWordRepo.AddAsync(blockedWord, ct);

        await _blockedWordService.ReloadAsync();

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = "AddBlockedWord",
            EntityType = "BlockedWord",
            EntityId = created.Id.ToString(),
            OldValue = null,
            NewValue = created.Word,
            CreatedAt = DateTime.UtcNow
        }, ct);

        return new BlockedWordResponse(created.Id, created.Word, created.CreatedAt);
    }

    public async Task RemoveBlockedWordAsync(Guid actorId, int id, CancellationToken ct = default)
    {
        var word = await _blockedWordRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Blocked word {id} not found.");

        await _blockedWordRepo.DeleteAsync(id, ct);

        await _blockedWordService.ReloadAsync();

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = "RemoveBlockedWord",
            EntityType = "BlockedWord",
            EntityId = id.ToString(),
            OldValue = word.Word,
            NewValue = null,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }

    // ── USER MANAGEMENT ─────────────────────────────────────────────────

    public async Task<PagedResult<AdminUserResponse>> GetUsersAsync(
        string? keyword, string? status, string? role, int page, int pageSize, CancellationToken ct = default)
    {
        AccountStatus? accountStatus = status is not null && Enum.TryParse<AccountStatus>(status, true, out var s)
            ? s : null;

        UserRoleType? roleType = role is not null && Enum.TryParse<UserRoleType>(role, true, out var r)
            ? r : null;

        var paged = await _userRepo.SearchAsync(keyword, accountStatus, roleType, page, pageSize, ct);

        var items = paged.Items.Select(u => new AdminUserResponse(
            u.Id,
            u.Email,
            u.Profile?.DisplayName,
            u.Profile?.AvatarUrl,
            u.Status.ToString(),
            u.Roles.Select(r => r.Role.ToString()).ToList(),
            u.CreatedAt)).ToList();

        return new PagedResult<AdminUserResponse>(items, paged.TotalCount, paged.Page, paged.PageSize, paged.TotalPages);
    }

    public async Task AssignRoleAsync(Guid actorId, Guid userId, AssignRoleRequest req, CancellationToken ct = default)
    {
        AssignRoleRequestValidator.Validate(req.Role);

        var user = await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException($"User {userId} not found.");

        var roleType = Enum.Parse<UserRoleType>(req.Role);

        if (user.Roles.Any(r => r.Role == roleType))
            throw new ConflictException("User already has this role.");

        var userRole = new UserRole
        {
            UserId = userId,
            Role = roleType,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepo.AddRoleAsync(userRole, ct);

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = "AssignRole",
            EntityType = "User",
            EntityId = userId.ToString(),
            OldValue = null,
            NewValue = req.Role,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }

    public async Task RemoveRoleAsync(Guid actorId, Guid userId, RemoveRoleRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Role))
            throw new DomainException("Role is required.");

        if (!Enum.TryParse<UserRoleType>(req.Role, out var roleType))
            throw new DomainException($"Invalid role: {req.Role}.");

        if (roleType == UserRoleType.User)
            throw new BadRequestException("Cannot remove the base User role.");

        if (roleType == UserRoleType.Admin && actorId == userId)
            throw new BadRequestException("Cannot remove your own Admin role.");

        var user = await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException($"User {userId} not found.");

        if (!user.Roles.Any(r => r.Role == roleType))
            throw new NotFoundException($"User does not have the {req.Role} role.");

        await _userRepo.RemoveRoleAsync(userId, roleType, ct);

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = "RemoveRole",
            EntityType = "User",
            EntityId = userId.ToString(),
            OldValue = req.Role,
            NewValue = null,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }

    public async Task SuspendUserAsync(Guid actorId, Guid userId, SuspendUserRequest req, CancellationToken ct = default)
    {
        SuspendUserRequestValidator.Validate(req.Reason);

        var user = await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException($"User {userId} not found.");

        if (user.Roles.Any(r => r.Role == UserRoleType.Admin))
            throw new ForbiddenException("Cannot suspend an Admin account.");

        if (user.Status == AccountStatus.Suspended)
            throw new BadRequestException("User account is already suspended.");

        if (user.Status != AccountStatus.Active)
            throw new BadRequestException("Only Active accounts can be suspended.");

        user.Status = AccountStatus.Suspended;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user, ct);

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = "SuspendAccount",
            EntityType = "User",
            EntityId = userId.ToString(),
            OldValue = null,
            NewValue = req.Reason,
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _notifications.NotifyUserAsync(
            userId,
            NotificationType.AccountSuspended,
            $"Your account has been suspended: {req.Reason}",
            "User",
            userId,
            ct);
    }

    public async Task ActivateUserAsync(Guid actorId, Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException($"User {userId} not found.");

        if (user.Status != AccountStatus.Suspended)
            throw new BadRequestException("Only Suspended accounts can be activated.");

        user.Status = AccountStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepo.UpdateAsync(user, ct);

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            Action = "ActivateAccount",
            EntityType = "User",
            EntityId = userId.ToString(),
            OldValue = null,
            NewValue = null,
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _notifications.NotifyUserAsync(
            userId,
            NotificationType.AccountActivated,
            "Your account has been reactivated.",
            "User",
            userId,
            ct);
    }
}
