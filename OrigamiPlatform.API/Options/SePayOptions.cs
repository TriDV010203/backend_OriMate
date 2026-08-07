namespace OrigamiPlatform.API.Options;

/// <summary>Bound from configuration section "SePay" — used only to verify inbound webhook calls.</summary>
public class SePayOptions
{
    public string WebhookApiKey { get; set; } = string.Empty;
}
