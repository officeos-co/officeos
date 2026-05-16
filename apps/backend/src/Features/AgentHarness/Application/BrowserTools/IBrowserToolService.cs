using OffceOs.Features.AgentHarness.Application.Tools;

namespace OffceOs.Features.AgentHarness.Application.BrowserTools;

internal interface IBrowserToolService
{
    Task<IReadOnlyList<IAgentTool>> CreateForTurnAsync(Guid agentId, CancellationToken ct = default);

    Task<IReadOnlyList<IAgentTool>> CreateCatalogAsync(Guid agentId, CancellationToken ct = default);
}
