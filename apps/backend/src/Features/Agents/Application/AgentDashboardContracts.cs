namespace OffceOs.Application.Features.Agents;

public interface IAgentDashboardService
{
    Task<AgentRecord> CreateAsync(CreateDashboardAgentRequest request, Guid ownerId, CancellationToken ct = default);
    Task<AgentRecord?> PatchAsync(Guid id, Guid ownerId, PatchAgentRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct = default);
    Task<IReadOnlyList<AgentToolPermissionRecord>> ListToolPermissionsAsync(Guid ownerId, Guid agentId, CancellationToken ct = default);
    Task<IReadOnlyList<AgentRunRecord>> ListRunsAsync(Guid ownerId, Guid agentId, Guid? parentRunId, CancellationToken ct = default);
    Task SetToolPermissionAsync(Guid ownerId, Guid agentId, string skill, string tool, ToolPermission mode, CancellationToken ct = default);
    Task<IReadOnlyList<AgentToolPermissionRecord>> SetToolPermissionsAsync(Guid ownerId, Guid agentId, IReadOnlyList<AgentToolPermissionRecord> rows, CancellationToken ct = default);
}

public sealed record CreateDashboardAgentRequest(
    string Name,
    string Provider,
    string? Model,
    string? Prompt,
    IReadOnlyList<string>? IntegrationSlugs,
    IReadOnlyList<string>? ChannelSlugs,
    IReadOnlyList<string>? ToolNames,
    IReadOnlyList<AgentToolPermissionInit>? ToolPermissions,
    IReadOnlyList<AgentResourceAttachmentRequest>? Resources,
    string? BootstrapMessage);

public sealed record AgentResourceAttachmentRequest(
    string ResourceType,
    Guid ResourceId,
    string? AccessMode,
    string? Instructions);
