namespace OffceOs.Domain.Common.ValueObjects;

public enum AgentStatus
{
    Booting,
    Restarting,
    Working,
    Idle,
    Failed,
}

public static class AgentStatusExtensions
{
    public static string ToStorageString(this AgentStatus status) => status switch
    {
        AgentStatus.Booting => "booting",
        AgentStatus.Restarting => "restarting",
        AgentStatus.Working => "working",
        AgentStatus.Idle => "idle",
        AgentStatus.Failed => "failed",
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };

    public static AgentStatus ToAgentStatus(this string value) => value switch
    {
        "booting" => AgentStatus.Booting,
        "pending" => AgentStatus.Booting,
        "restarting" => AgentStatus.Restarting,
        "working" => AgentStatus.Working,
        "idle" => AgentStatus.Idle,
        "running" => AgentStatus.Idle,
        "failed" => AgentStatus.Failed,
        "stopped" => AgentStatus.Failed,
        "not_found" => AgentStatus.Failed,
        "unknown" => AgentStatus.Failed,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown agent status: {value}"),
    };
}
