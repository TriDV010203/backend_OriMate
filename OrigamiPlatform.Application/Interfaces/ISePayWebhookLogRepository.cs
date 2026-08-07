using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Application.Interfaces;

public interface ISePayWebhookLogRepository
{
    Task<bool> ExistsBySePayTransactionIdAsync(long sePayTransactionId, CancellationToken ct = default);

    Task AddAsync(SePayWebhookLog log, CancellationToken ct = default);
}
