namespace OffceOs.Features.Browser.Domain;

public interface IBrowserSessionRepository
{
    Task<BrowserSessionRecord?> GetByAsync(BrowserSessionFilter filter, CancellationToken ct = default);
    Task<BrowserSessionRecord> UpsertAsync(Guid agentId, string runtimeSessionId, string? cookiesJson, CancellationToken ct = default);
    Task DeleteByAgentAsync(Guid agentId, CancellationToken ct = default);
}
