namespace OffceOs.Api.Features.Agents;

public sealed record AgentPayload(
    Guid Id,
    string Name,
    string Provider,
    string? Model,
    string Status,
    string? PodName,
    string? ServiceUrl,
    string? Prompt,
    DateTime CreatedAt,
    bool IsDeleted,
    Guid? OwnerId,
    Guid? WorkspaceId,
    Guid? ActiveDefinitionId,
    string? LastRelevantMessage,
    IReadOnlyList<AgentPersonalityRecord> PersonalityFiles,
    IReadOnlyList<AgentMemoryRecord> Memories,
    IReadOnlyList<AgentRateLimitRecord> RateLimits,
    IReadOnlyList<AgentChannelBindingRecord> ChannelBindings,
    AgentSessionRecord? ActiveSession);

internal static class AgentGraphQLMapper
{
    public static AgentPayload ToPayload(
        AgentRecord agent,
        AgentStatus status,
        string? lastRelevantMessage) => new(
        agent.Id,
        agent.Name,
        agent.Provider,
        agent.Model,
        status.ToStorageString(),
        agent.PodName,
        agent.ServiceUrl,
        agent.Prompt,
        agent.CreatedAt,
        agent.IsDeleted,
        agent.OwnerId,
        agent.WorkspaceId,
        agent.ActiveDefinitionId,
        lastRelevantMessage,
        agent.PersonalityFiles,
        agent.Memories,
        agent.RateLimits,
        agent.ChannelBindings,
        agent.ActiveSession);
}
