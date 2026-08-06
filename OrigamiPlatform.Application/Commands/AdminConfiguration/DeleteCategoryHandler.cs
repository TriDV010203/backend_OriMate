using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

// Soft delete: Category.CategoryId has DeleteBehavior.Restrict on Tutorial, so a hard
// delete would fail (or orphan data) for any category still referenced by a tutorial.
// We flag IsDeleted/IsActive instead — the row stays for FK integrity and existing
// tutorials keep their categoryName; the category just disappears from every list.
public class DeleteCategoryHandler
{
    private readonly ICategoryRepository _categoryRepo;
    private readonly IAuditLogRepository _auditLog;

    public DeleteCategoryHandler(ICategoryRepository categoryRepo, IAuditLogRepository auditLog)
        => (_categoryRepo, _auditLog) = (categoryRepo, auditLog);

    public async Task HandleAsync(DeleteCategoryCommand command, CancellationToken ct = default)
    {
        var category = await _categoryRepo.GetByIdAsync(command.CategoryId, ct)
            ?? throw new NotFoundException($"Category {command.CategoryId} not found.");

        category.IsDeleted = true;
        category.IsActive = false;
        category.UpdatedAt = DateTime.UtcNow;

        await _categoryRepo.UpdateAsync(category, ct);

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = command.ActorId,
            Action = "DeleteCategory",
            EntityType = "Category",
            EntityId = command.CategoryId.ToString(),
            OldValue = category.Name,
            NewValue = null,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }
}
