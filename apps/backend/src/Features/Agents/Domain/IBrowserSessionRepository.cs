namespace EnterpriseAgentOs.Domain.Features.Agents;

public sealed record BrowserSessionFilter
{
    public Guid? Id { get; init; }
    public Guid? AgentId { get; init; }
    public string? RuntimeSessionId { get; init; }
}

public interface IBrowserSessionRepository
{
    Task<BrowserSessionRecord?> GetByAsync(BrowserSessionFilter filter, CancellationToken ct = default);
    Task<BrowserSessionRecord> UpsertAsync(Guid agentId, string runtimeSessionId, string? cookiesJson, CancellationToken ct = default);
    Task DeleteByAgentAsync(Guid agentId, CancellationToken ct = default);
}
