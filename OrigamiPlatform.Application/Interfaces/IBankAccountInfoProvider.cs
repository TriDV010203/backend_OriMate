namespace OrigamiPlatform.Application.Interfaces;

/// <summary>Platform bank account that receives VIP subscription transfers, matched by SePay.</summary>
public interface IBankAccountInfoProvider
{
    string AccountNumber { get; }
    string BankName { get; }
    string BankBin { get; }
    string AccountHolderName { get; }
}
