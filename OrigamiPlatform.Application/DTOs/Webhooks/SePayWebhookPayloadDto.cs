namespace OrigamiPlatform.Application.DTOs.Webhooks;

/// <summary>Shape of the JSON body SePay POSTs to /api/webhooks/sepay on every bank transaction.</summary>
public record SePayWebhookPayloadDto(
    long Id,
    string Gateway,
    string? TransactionDate,
    string? AccountNumber,
    string? SubAccount,
    string? Code,
    string Content,
    string TransferType,
    decimal TransferAmount,
    decimal? Accumulated,
    string? ReferenceCode,
    string? Description
);
