using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public class RemoveBlockedWordHandler
{
    private readonly IBlockedWordRepository _blockedWordRepo;
    private readonly IBlockedWordService _blockedWordService;
    private readonly IAuditLogRepository _auditLog;

    public RemoveBlockedWordHandler(
        IBlockedWordRepository blockedWordRepo,
        IBlockedWordService blockedWordService,
        IAuditLogRepository auditLog)
        => (_blockedWordRepo, _blockedWordService, _auditLog) = (blockedWordRepo, blockedWordService, auditLog);

    public async Task HandleAsync(RemoveBlockedWordCommand command, CancellationToken ct = default)
    {
        var word = await _blockedWordRepo.GetByIdAsync(command.BlockedWordId, ct)
            ?? throw new NotFoundException($"Blocked word {command.BlockedWordId} not found.");

        await _blockedWordRepo.DeleteAsync(command.BlockedWordId, ct);

        await _blockedWordService.ReloadAsync();

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = command.ActorId,
            Action = "RemoveBlockedWord",
            EntityType = "BlockedWord",
            EntityId = command.BlockedWordId.ToString(),
            OldValue = word.Word,
            NewValue = null,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }
}
