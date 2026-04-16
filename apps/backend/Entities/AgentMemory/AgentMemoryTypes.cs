namespace EnterpriseAgentOs.Api.Entities.AgentMemory.Types;

public sealed record AgentMemoryGqlDto(
    Guid Id,
    Guid AgentId,
    string Key,
    string Content,
    string Category,
    string Namespace,
    string? SessionId,
    double? Importance,
    string? SupersededBy,
    DateTime CreatedAt);

public sealed record VaultFileDto(
    string Path,
    string Content);

internal static class AgentMemoryGraphQLMapper
{
    public static AgentMemoryGqlDto ToDto(AgentMemoryRecord m) => new(
        m.Id, m.AgentId, m.Key, m.Content, m.Category, m.Namespace,
        m.SessionId, m.Importance, m.SupersededBy, m.CreatedAt);
}
