namespace OffceOs.Domain.Common.ValueObjects;

public enum BillingCycle
{
    Monthly,
    Yearly,
}

public static class BillingCycleExtensions
{
    public static string ToStorageString(this BillingCycle cycle) => cycle switch
    {
        BillingCycle.Monthly => "monthly",
        BillingCycle.Yearly => "yearly",
        _ => throw new ArgumentOutOfRangeException(nameof(cycle)),
    };

    public static BillingCycle ToBillingCycle(this string value) => value switch
    {
        "monthly" => BillingCycle.Monthly,
        "yearly" => BillingCycle.Yearly,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown billing cycle: {value}"),
    };
}
