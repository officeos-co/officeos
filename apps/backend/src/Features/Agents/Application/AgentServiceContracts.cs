namespace OffceOs.Application.Features.Agents;

public interface IAgentService
{
    Task<IReadOnlyList<AgentRecord>> ListAsync(AgentFilter filter, CancellationToken ct = default);
    Task<AgentRecord?> GetByAsync(AgentFilter filter, CancellationToken ct = default);
    Task<AgentRecord> CreateAsync(CreateAgentRequest request, Guid? ownerId = null, Guid? workspaceId = null, CancellationToken ct = default);
    Task<AgentRecord?> PatchAsync(Guid id, PatchAgentRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task InitializeAgentAsync(Guid agentId, Guid userId, AgentInitRequest init, CancellationToken ct = default);
}

public sealed record CreateAgentRequest(
    string Name,
    string Provider,
    string? Model,
    string? Prompt = null);

public sealed record PatchAgentRequest(string? Provider, string? Model, string? Name = null, string? Prompt = null);

public sealed record AgentInitRequest(
    IReadOnlyList<string>? ToolNames,
    IReadOnlyList<AgentToolPermissionInit>? ToolPermissions,
    IReadOnlyList<Guid>? ChannelConnectionIds,
    string? BootstrapMessage);

public sealed record AgentToolPermissionInit(string Tool, ToolPermission Mode);
