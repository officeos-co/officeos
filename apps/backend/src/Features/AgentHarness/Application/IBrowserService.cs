using OffceOs.Domain.Features.Browser;

namespace OffceOs.Application.Features.AgentHarness;

public interface IBrowserService
{
    Task<BrowserSessionState> GetOrCreateAsync(Guid agentId, CancellationToken ct = default);
    Task<BrowserSessionState?> GetStateAsync(Guid agentId, CancellationToken ct = default);
    Task<BrowserSessionState> RestartAsync(Guid agentId, CancellationToken ct = default);
    Task StopAsync(Guid agentId, CancellationToken ct = default);
    Task<string?> GetViewUrlAsync(Guid agentId, CancellationToken ct = default);
}
