using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;
using OrigamiPlatform.Domain.Exceptions;

namespace OrigamiPlatform.Application.Commands.AdminConfiguration;

public class SetOfficialTutorialHandler
{
    private readonly ITutorialRepository _tutorials;
    private readonly IAuditLogRepository _auditLog;

    public SetOfficialTutorialHandler(ITutorialRepository tutorials, IAuditLogRepository auditLog)
        => (_tutorials, _auditLog) = (tutorials, auditLog);

    public async Task HandleAsync(SetOfficialTutorialCommand command, CancellationToken ct = default)
    {
        var tutorial = await _tutorials.GetByIdWithStepsAsync(command.TutorialId, ct)
            ?? throw new NotFoundException($"Tutorial {command.TutorialId} not found.");

        var oldValue = tutorial.IsOfficial;
        tutorial.IsOfficial = command.IsOfficial;
        tutorial.UpdatedAt = DateTime.UtcNow;

        await _tutorials.UpdateAsync(tutorial, ct);

        await _auditLog.LogAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorId = command.ActorId,
            Action = "SetOfficialTutorial",
            EntityType = "Tutorial",
            EntityId = command.TutorialId.ToString(),
            OldValue = oldValue.ToString(),
            NewValue = command.IsOfficial.ToString(),
            CreatedAt = DateTime.UtcNow
        }, ct);
    }
}
