namespace EnterpriseAgentOs.Application.Features.Agents;

public interface IAgentService
{
    Task<IReadOnlyList<AgentResult>> ListAsync(AgentFilter filter, CancellationToken ct = default);
    Task<AgentResult?> GetByAsync(AgentFilter filter, CancellationToken ct = default);
    Task<AgentResult> CreateAsync(CreateAgentRequest request, Guid? ownerId = null, CancellationToken ct = default);
    Task<AgentResult?> PatchAsync(Guid id, PatchAgentRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task InitializeAgentAsync(Guid agentId, Guid userId, AgentInitRequest init, CancellationToken ct = default);
}

public sealed record AgentResult(
    Guid Id,
    string Name,
    string Provider,
    string? Model,
    string? Prompt,
    string Status,
    string? PodName,
    string? ServiceUrl,
    DateTime CreatedAt);

public sealed record CreateAgentRequest(
    string Name,
    string Provider,
    string? Model,
    string? Prompt = null);

public sealed record PatchAgentRequest(string? Provider, string? Model, string? Name = null, string? Prompt = null);

public sealed record AgentInitRequest(
    IReadOnlyList<string>? ToolNames,
    IReadOnlyList<AgentToolPermissionInit>? ToolPermissions,
    IReadOnlyList<string>? ChannelSlugs,
    string? BootstrapMessage);

public sealed record AgentToolPermissionInit(string Tool, ToolPermission Mode);
