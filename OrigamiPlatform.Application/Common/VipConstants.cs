namespace OrigamiPlatform.Application.Common;

/// <summary>Shared constants for the platform-wide VIP subscription model.</summary>
public static class VipConstants
{
    /// <summary>Platform-fixed VIP subscription price (VND). Not creator-configurable.</summary>
    public const decimal FixedPriceVnd = 50000m;

    /// <summary>Platform commission taken from each VIP subscription payment.</summary>
    public const decimal PlatformCommissionRate = 0.10m;
}
