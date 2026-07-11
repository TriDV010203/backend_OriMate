namespace OrigamiPlatform.Application.DTOs.Subscriptions;

public record TransactionDto(
    Guid Id,
    Guid UserId,
    Guid? CreatorId,
    string TransactionType,
    decimal Amount,
    string Status,
    string? ReferenceCode,
    Guid? ConfirmedBy,
    DateTime? ConfirmedAt,
    string? AdminNote,
    DateTime CreatedAt
);
