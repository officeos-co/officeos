namespace OffceOs.Domain.Features.ResourceLogs;

public static class ResourceLogTopics
{
    private const string ResourceLogAppendedPrefix = "resource-log-appended";

    public static string ResourceLogAppended(Guid agentId) => $"{ResourceLogAppendedPrefix}:{agentId:N}";
}
