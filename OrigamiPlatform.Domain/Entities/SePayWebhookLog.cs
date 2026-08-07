using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class SePayWebhookLog
{
    public Guid Id { get; set; }
    public Guid? TransactionId { get; set; }
    public long SePayTransactionId { get; set; }
    public string Gateway { get; set; } = null!;
    public decimal TransferAmount { get; set; }
    public string Content { get; set; } = null!;
    public SePayWebhookMatchResult MatchResult { get; set; }
    public string RawPayload { get; set; } = null!;
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Transaction? Transaction { get; set; }
}
