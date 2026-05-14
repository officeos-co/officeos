namespace OffceOs.Domain.Features.Observability;

public static class AgentLogTopics
{
    private const string AgentLogAppendedPrefix = "agent-log-appended";

    public static string AgentLogAppended(Guid agentId) => $"{AgentLogAppendedPrefix}:{agentId:N}";
}
