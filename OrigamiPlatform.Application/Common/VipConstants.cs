namespace OrigamiPlatform.Application.Common;

/// <summary>Shared constants for the platform-wide VIP subscription model.</summary>
public static class VipConstants
{
    /// <summary>Platform-fixed VIP subscription price (VND). Not creator-configurable.</summary>
    public const decimal FixedPriceVnd = 30000m;

    /// <summary>Platform commission taken from each VIP subscription payment.</summary>
    public const decimal PlatformCommissionRate = 0.10m;

    /// <summary>BR-VIP-06: minimum number of Published tutorials a creator must have before enabling VIP selling.</summary>
    public const int MinPublishedTutorialsToSell = 5;

    /// <summary>Prefix for system-generated Transaction.PaymentCode, matched against SePay webhook content.</summary>
    public const string PaymentCodePrefix = "OMVIP";
}
