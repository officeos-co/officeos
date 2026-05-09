namespace OffceOs.Domain.Common.ValueObjects;

/// <summary>
/// Represents a billing period with a start and end date.
/// </summary>
public readonly record struct BillingPeriod(DateTime Start, DateTime End)
{
    public bool IsExpired => DateTime.UtcNow >= End;
    public bool IsCurrent => DateTime.UtcNow >= Start && DateTime.UtcNow < End;
    public TimeSpan Duration => End - Start;

    public static BillingPeriod CurrentMonth()
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return new BillingPeriod(start, start.AddMonths(1));
    }
}
