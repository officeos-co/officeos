using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.ResourceLogs;
namespace OffceOs.Application.Features.Agents;

public interface IAgentService
{
    Task<IReadOnlyList<AgentRecord>> ListAsync(AgentFilter filter, CancellationToken ct = default);
    Task<AgentRecord?> GetByAsync(AgentFilter filter, CancellationToken ct = default);
    Task<AgentRecord> CreateAsync(CreateAgentRequest request, Guid? ownerId = null, Guid? workspaceId = null, CancellationToken ct = default);
    Task<AgentRecord?> PatchAsync(Guid id, PatchAgentRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task InitializeAgentAsync(Guid agentId, Guid userId, AgentInitRequest init, CancellationToken ct = default);
    Task<ResourceLogRecord> SendMessageAsync(
        Guid agentId,
        string content,
        Guid userId,
        CancellationToken ct = default,
        string? runPurpose = null,
        Guid? definitionId = null);
}

public sealed record CreateAgentRequest(
    string Name,
    string Provider,
    string? Model,
    string? Prompt = null,
    string? ConfigJson = null);

public sealed record PatchAgentRequest(string? Provider, string? Model, string? Name = null, string? Prompt = null, string? ConfigJson = null);

public sealed record AgentInitRequest(
    IReadOnlyList<string>? ToolNames,
    IReadOnlyList<Guid>? ChannelConnectionIds,
    string? BootstrapMessage);
