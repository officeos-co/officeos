namespace OffceOs.Features.Agents.Domain;

public enum SessionStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Canceled,
}

public static class SessionStatusExtensions
{
    public static string ToStorageString(this SessionStatus status) => status switch
    {
        SessionStatus.Queued => "queued",
        SessionStatus.Running => "running",
        SessionStatus.Completed => "completed",
        SessionStatus.Failed => "failed",
        SessionStatus.Canceled => "canceled",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static SessionStatus ToSessionStatus(this string value) => value switch
    {
        "queued" => SessionStatus.Queued,
        "running" => SessionStatus.Running,
        "completed" => SessionStatus.Completed,
        "failed" => SessionStatus.Failed,
        "canceled" => SessionStatus.Canceled,
        "active" => SessionStatus.Running,
        "ended" => SessionStatus.Completed,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown session status: {value}"),
    };
}

public static class AgentSessionSourceKinds
{
    public const string Manual = "manual";
    public const string Routine = "routine";
    public const string Channel = "channel";
    public const string GitHub = "github";

    public static string Normalize(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return Manual;

        var normalized = source.Trim().ToLowerInvariant();
        return normalized is Manual or Routine or Channel or GitHub
            ? normalized
            : throw new ArgumentOutOfRangeException(nameof(source), $"Unknown session source: {source}");
    }
}
