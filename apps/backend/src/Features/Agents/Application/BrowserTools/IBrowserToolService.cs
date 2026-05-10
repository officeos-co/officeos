namespace OffceOs.Application.Features.Agents;

internal interface IBrowserToolService
{
    Task<IReadOnlyList<IAgentTool>> CreateForTurnAsync(Guid agentId, CancellationToken ct = default);

    Task<IReadOnlyList<IAgentTool>> CreateCatalogAsync(Guid agentId, CancellationToken ct = default);
}
