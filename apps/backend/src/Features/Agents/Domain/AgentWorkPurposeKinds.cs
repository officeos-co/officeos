namespace OffceOs.Domain.Features.Agents;

public static class AgentWorkPurposeKinds
{
    public const string Manual = "manual";
    public const string Bootstrap = "bootstrap";
    public const string Channel = "channel";
    public const string Routine = "routine";

    public static string Normalize(string? purpose) =>
        string.IsNullOrWhiteSpace(purpose) ? Manual : purpose.Trim().ToLowerInvariant();
}

public static class AgentWorkStatusKinds
{
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Canceled = "canceled";

    public static string Normalize(string? status) =>
        string.IsNullOrWhiteSpace(status) ? Queued : status.Trim().ToLowerInvariant();
}
