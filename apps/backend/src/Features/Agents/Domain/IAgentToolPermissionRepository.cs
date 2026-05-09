namespace OffceOs.Domain.Features.Agents;

public interface IAgentToolPermissionRepository
{
    Task<IReadOnlyList<AgentToolPermissionRecord>> ListForAgentAsync(Guid agentId, CancellationToken ct = default);
    Task UpsertAsync(Guid agentId, string skillName, string toolName, ToolPermission permission, CancellationToken ct = default);
    Task SetManyAsync(Guid agentId, IReadOnlyList<AgentToolPermissionRecord> entries, CancellationToken ct = default);
}
