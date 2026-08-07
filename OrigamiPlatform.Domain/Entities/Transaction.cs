using OrigamiPlatform.Domain.Enums;

namespace OrigamiPlatform.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal PlatformFeeAmount { get; set; }
    public decimal CreatorNetAmount { get; set; }
    public TransactionStatus Status { get; set; }
    public string PaymentCode { get; set; } = null!;
    public Guid? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? AdminNote { get; set; }
    public Guid? CreatorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public User? ConfirmedByUser { get; set; }
    public User? Creator { get; set; }
}
