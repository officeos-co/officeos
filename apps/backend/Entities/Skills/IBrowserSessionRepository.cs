namespace EnterpriseAgentOs.Api.Entities.Skills;

public interface IBrowserSessionRepository
{
    Task<EnterpriseAgentOs.Api.Database.Models.BrowserSessionRecord?> GetByAgentAsync(Guid agentId, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.BrowserSessionRecord> UpsertAsync(Guid agentId, string runtimeSessionId, string? cookiesJson, CancellationToken ct = default);
    Task DeleteByAgentAsync(Guid agentId, CancellationToken ct = default);
}
