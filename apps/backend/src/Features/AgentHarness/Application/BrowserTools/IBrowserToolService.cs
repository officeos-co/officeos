namespace OffceOs.Application.Features.AgentHarness;

internal interface IBrowserToolService
{
    Task<IReadOnlyList<IAgentTool>> CreateForTurnAsync(Guid agentId, CancellationToken ct = default);

    Task<IReadOnlyList<IAgentTool>> CreateCatalogAsync(Guid agentId, CancellationToken ct = default);
}
