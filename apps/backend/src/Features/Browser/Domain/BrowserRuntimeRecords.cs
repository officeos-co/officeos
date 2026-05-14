namespace OffceOs.Domain.Features.Browser;

public sealed record BrowserSessionState(
    Guid AgentId,
    string? RuntimeSessionId,
    string Status,
    string? Name,
    string? CurrentUrl,
    string? Title,
    string? TakeoverUrl,
    DateTime? CreatedAt,
    DateTime? LastAccessedAt);

public sealed record BrowserToolDescriptor(
    string Name,
    string Description,
    JsonElement InputSchema);

public sealed record BrowserToolCallResult(
    bool IsError,
    string Output);

public interface IBrowserRuntimeClient
{
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
    Task<BrowserSessionState?> GetSessionAsync(Guid agentId, string runtimeSessionId, CancellationToken ct = default);
    Task<BrowserSessionState> CreateSessionAsync(Guid agentId, string name, string? authProfile, CancellationToken ct = default);
    Task CloseSessionAsync(string runtimeSessionId, CancellationToken ct = default);
    Task<IReadOnlyList<BrowserToolDescriptor>> ListToolsAsync(CancellationToken ct = default);
    Task<BrowserToolCallResult> CallToolAsync(string name, Dictionary<string, object?> arguments, CancellationToken ct = default);
}
