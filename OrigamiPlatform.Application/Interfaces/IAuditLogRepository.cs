using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface IAuditLogRepository
{
    Task LogAsync(AuditLog log, CancellationToken ct = default);
}
