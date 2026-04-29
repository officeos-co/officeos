namespace EnterpriseAgentOs.Domain.Common.ValueObjects;

public enum SessionStatus
{
    Active,
    Ended,
}

public static class SessionStatusExtensions
{
    public static string ToStorageString(this SessionStatus status) => status switch
    {
        SessionStatus.Active => "active",
        SessionStatus.Ended => "ended",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static SessionStatus ToSessionStatus(this string value) => value switch
    {
        "active" => SessionStatus.Active,
        "ended" => SessionStatus.Ended,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown session status: {value}"),
    };
}
