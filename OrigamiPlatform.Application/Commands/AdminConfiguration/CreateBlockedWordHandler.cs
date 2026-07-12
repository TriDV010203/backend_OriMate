using OrigamiPlatform.Application.Features.AdminConfiguration.DTOs;
using OrigamiPlatform.Application.Features.AdminConfiguration.Validators;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public class CreateBlockedWordHandler
{
    private readonly IBlockedWordRepository _blockedWordRepo;
    private readonly IBlockedWordService _blockedWordService;
    private readonly IAuditLogRepository _auditLog;

    public CreateBlockedWordHandler(
        IBlockedWordRepository blockedWordRepo,
        IBlockedWordService blockedWordService,
        IAuditLogRepository auditLog)
        => (_blockedWordRepo, _blockedWordService, _auditLog) = (blockedWordRepo, blockedWordService, auditLog);

    public async Task<BlockedWordResponse> HandleAsync(CreateBlockedWordCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
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
            ActorId = command.ActorId,
            Action = "AddBlockedWord",
            EntityType = "BlockedWord",
            EntityId = created.Id.ToString(),
            OldValue = null,
            NewValue = created.Word,
            CreatedAt = DateTime.UtcNow
        }, ct);

        return new BlockedWordResponse(created.Id, created.Word, created.CreatedAt);
    }
}
