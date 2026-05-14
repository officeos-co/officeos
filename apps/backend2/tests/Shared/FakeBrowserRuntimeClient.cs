using OffceOs.Domain.Features.Agents;

namespace OffceOs.Tests.Shared;

public sealed class FakeBrowserRuntimeClient : IBrowserRuntimeClient
{
    public string? LastToolName { get; private set; }
    public Dictionary<string, object?>? LastArguments { get; private set; }

    public Task<bool> IsAvailableAsync(CancellationToken ct = default) => Task.FromResult(true);

    public Task<BrowserSessionState?> GetSessionAsync(Guid agentId, string runtimeSessionId, CancellationToken ct = default) =>
        Task.FromResult<BrowserSessionState?>(null);

    public Task<BrowserSessionState> CreateSessionAsync(Guid agentId, string name, string? authProfile, CancellationToken ct = default) =>
        Task.FromResult(new BrowserSessionState(agentId, "session-1", "active", name, null, null, null, null, null));

    public Task CloseSessionAsync(string runtimeSessionId, CancellationToken ct = default) => Task.CompletedTask;

    public Task<IReadOnlyList<BrowserToolDescriptor>> ListToolsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<BrowserToolDescriptor>>([]);

    public Task<BrowserToolCallResult> CallToolAsync(string name, Dictionary<string, object?> arguments, CancellationToken ct = default)
    {
        LastToolName = name;
        LastArguments = arguments;
        return Task.FromResult(new BrowserToolCallResult(false, "{}"));
    }
}
