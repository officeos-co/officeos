namespace OffceOs.Application.Features.Agents;

public interface IAgentToolCatalogService
{
    Task<IReadOnlyList<AgentToolCatalogEntry>> ListAsync(Guid? agentId, CancellationToken ct = default);
}
