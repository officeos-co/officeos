namespace OffceOs.Features.AgentRoutines.Domain;

/// <summary>
/// A validated cron expression (5 or 6 fields).
/// </summary>
public readonly record struct CronExpression
{
    public string Value { get; }

    public CronExpression(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Cron expression cannot be empty.", nameof(value));

        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5 || parts.Length > 6)
            throw new ArgumentException($"Cron expression must have 5 or 6 fields, got {parts.Length}: {value}", nameof(value));

        Value = value.Trim();
    }

    public override string ToString() => Value;

    public static implicit operator string(CronExpression expr) => expr.Value;
    public static explicit operator CronExpression(string value) => new(value);
}
