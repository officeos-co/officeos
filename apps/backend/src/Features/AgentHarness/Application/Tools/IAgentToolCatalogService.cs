namespace OffceOs.Features.AgentHarness.Application.Tools;

public interface IAgentToolCatalogService
{
    Task<IReadOnlyList<AgentToolCatalogEntry>> ListAsync(Guid? agentId, CancellationToken ct = default);
}
