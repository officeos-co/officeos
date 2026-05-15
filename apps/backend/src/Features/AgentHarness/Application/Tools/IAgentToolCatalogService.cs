namespace OffceOs.Application.Features.AgentHarness;

public interface IAgentToolCatalogService
{
    Task<IReadOnlyList<AgentToolCatalogEntry>> ListAsync(Guid? agentId, CancellationToken ct = default);
}
