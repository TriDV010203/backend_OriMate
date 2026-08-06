namespace OrigamiPlatform.Application.Commands.Webhooks;

public record ProcessSePayWebhookCommand(
    long SePayTransactionId,
    string Gateway,
    string TransferType,
    decimal TransferAmount,
    string Content,
    string? Code,
    string RawPayload
);
