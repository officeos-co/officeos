namespace OffceOs.Application.Features.Agents;

public interface IAgentLifecycleService
{
    Task<IReadOnlyList<AgentLifecycleResult>> ListAgentsAsync(Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<AgentLifecycleResult?> GetAgentAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<AgentRecord> CreateAsync(CreateAgentLifecycleRequest request, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<AgentRecord?> PatchAsync(Guid id, Guid ownerId, Guid workspaceId, PatchAgentRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<IReadOnlyList<AgentRunRecord>> ListRunsAsync(Guid ownerId, Guid workspaceId, Guid agentId, Guid? parentRunId, CancellationToken ct = default);
}

public sealed record AgentLifecycleResult(
    AgentRecord Agent,
    AgentStatus Status,
    string? LastRelevantMessage);

public sealed record CreateAgentLifecycleRequest(
    string Name,
    string Provider,
    string? Model,
    string? Prompt,
    string? ConfigJson,
    IReadOnlyList<string>? IntegrationSlugs,
    IReadOnlyList<Guid>? ChannelConnectionIds,
    IReadOnlyList<string>? ToolNames,
    IReadOnlyList<AgentResourceAttachmentRequest>? Resources,
    string? BootstrapMessage);

public sealed record AgentResourceAttachmentRequest(
    string ResourceType,
    Guid ResourceId,
    string? AccessMode,
    string? Instructions);
