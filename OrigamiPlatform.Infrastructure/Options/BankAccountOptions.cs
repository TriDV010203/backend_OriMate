namespace OrigamiPlatform.Infrastructure.Options;

/// <summary>Bound from configuration section "BankAccount" — the platform's SePay-linked bank account.</summary>
public class BankAccountOptions
{
    public string AccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankBin { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
}
