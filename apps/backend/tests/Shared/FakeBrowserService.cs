using OffceOs.Features.AgentHarness.Application;
using OffceOs.Features.Browser.Domain;

namespace OffceOs.Tests.Shared;

public sealed class FakeBrowserService : IBrowserService
{
    public Task<BrowserSessionState> GetOrCreateAsync(Guid agentId, CancellationToken ct = default) =>
        Task.FromResult(new BrowserSessionState(agentId, "session-1", "active", null, null, null, null, null, null));

    public Task<BrowserSessionState?> GetStateAsync(Guid agentId, CancellationToken ct = default) =>
        Task.FromResult<BrowserSessionState?>(null);

    public Task<BrowserSessionState> RestartAsync(Guid agentId, CancellationToken ct = default) =>
        GetOrCreateAsync(agentId, ct);

    public Task StopAsync(Guid agentId, CancellationToken ct = default) => Task.CompletedTask;

    public Task<string?> GetViewUrlAsync(Guid agentId, CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
}
