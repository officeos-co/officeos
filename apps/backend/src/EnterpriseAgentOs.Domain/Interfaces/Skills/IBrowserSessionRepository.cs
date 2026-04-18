namespace EnterpriseAgentOs.Domain.Interfaces.Skills;

public interface IBrowserSessionRepository
{
    Task<EnterpriseAgentOs.Domain.Models.BrowserSessionRecord?> GetByAgentAsync(Guid agentId, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.BrowserSessionRecord> UpsertAsync(Guid agentId, string runtimeSessionId, string? cookiesJson, CancellationToken ct = default);
    Task DeleteByAgentAsync(Guid agentId, CancellationToken ct = default);
}
