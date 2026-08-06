using Microsoft.EntityFrameworkCore;
using OrigamiPlatform.Application.Interfaces;
using OrigamiPlatform.Domain.Entities;

namespace OrigamiPlatform.Infrastructure.Persistence.Repositories;

public class SePayWebhookLogRepository : ISePayWebhookLogRepository
{
    private readonly AppDbContext _db;

    public SePayWebhookLogRepository(AppDbContext db) => _db = db;

    public Task<bool> ExistsBySePayTransactionIdAsync(long sePayTransactionId, CancellationToken ct = default)
        => _db.SePayWebhookLogs.AnyAsync(l => l.SePayTransactionId == sePayTransactionId, ct);

    public async Task AddAsync(SePayWebhookLog log, CancellationToken ct = default)
    {
        _db.SePayWebhookLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
