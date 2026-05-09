namespace OffceOs.Domain.Common.ValueObjects;

public enum AgentStatus
{
    Pending,
    Running,
    Failed,
}

public static class AgentStatusExtensions
{
    public static string ToStorageString(this AgentStatus status) => status switch
    {
        AgentStatus.Pending => "pending",
        AgentStatus.Running => "running",
        AgentStatus.Failed => "failed",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static AgentStatus ToAgentStatus(this string value) => value switch
    {
        "pending" => AgentStatus.Pending,
        "running" => AgentStatus.Running,
        "failed" => AgentStatus.Failed,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown agent status: {value}"),
    };
}
